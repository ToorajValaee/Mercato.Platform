using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;
using Nop.Services.Logging;

namespace Mercato.OrderSync.Plugin;

public sealed class OrderPaidConsumer : IConsumer<OrderPaidEvent>
{
    private readonly NopOrderSyncService _sync;
    private readonly ILogger _logger;

    public OrderPaidConsumer(NopOrderSyncService sync, ILogger logger)
    {
        _sync = sync;
        _logger = logger;
    }

    public async Task HandleEventAsync(OrderPaidEvent eventMessage)
    {
        var order = eventMessage.Order;
        try
        {
            await _sync.SyncAsync(order);
        }
        catch (Exception exception)
        {
            await _logger.InsertLogAsync(
                LogLevel.Error,
                $"Mercato OrderSync failed for nopCommerce order {order.Id}",
                exception.ToString());
            throw;
        }
    }
}
