using Mercato.NopCommerce.Core;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.ScheduleTasks;

namespace Mercato.OrderSync.Plugin;

public sealed class OrderSyncRetryTask : IScheduleTask
{
    private const int BatchSize = 100;

    private readonly IOrderService _orders;
    private readonly IGenericAttributeService _attributes;
    private readonly NopOrderSyncService _sync;
    private readonly ILogger _logger;

    public OrderSyncRetryTask(
        IOrderService orders,
        IGenericAttributeService attributes,
        NopOrderSyncService sync,
        ILogger logger)
    {
        _orders = orders;
        _attributes = attributes;
        _sync = sync;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var pageIndex = 0;
        while (true)
        {
            var paidOrders = await _orders.SearchOrdersAsync(
                psIds: [(int)PaymentStatus.Paid],
                pageIndex: pageIndex,
                pageSize: BatchSize);

            foreach (var order in paidOrders)
            {
                var syncedUtc = await _attributes.GetAttributeAsync<string>(
                    order,
                    MercatoNopDefaults.OrderSyncedUtcAttribute,
                    order.StoreId);
                if (!string.IsNullOrWhiteSpace(syncedUtc))
                    continue;

                try
                {
                    await _sync.SyncAsync(order);
                }
                catch (Exception exception)
                {
                    await _logger.InsertLogAsync(
                        LogLevel.Error,
                        $"Mercato OrderSync retry failed for nopCommerce order {order.Id}",
                        exception.ToString());
                }
            }

            if (paidOrders.Count < BatchSize)
                break;
            pageIndex++;
        }
    }
}
