using Mercato.NopCommerce.Core;
using Microsoft.Extensions.Configuration;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Orders;

namespace Mercato.OrderSync.Plugin;

public sealed class OrderPaidConsumer : IConsumer<OrderPaidEvent>
{
    public const string BranchAttribute = "Mercato.BranchId";
    public const string CustomerAttribute = "Mercato.CustomerId";
    private const string ProductIdPrefix = "Mercato.ProductId=";

    private readonly OrderSyncCore _sync;
    private readonly IOrderService _orders;
    private readonly IProductService _products;
    private readonly IGenericAttributeService _attributes;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public OrderPaidConsumer(
        OrderSyncCore sync,
        IOrderService orders,
        IProductService products,
        IGenericAttributeService attributes,
        IConfiguration configuration,
        ILogger logger)
    {
        _sync = sync;
        _orders = orders;
        _products = products;
        _attributes = attributes;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task HandleEventAsync(OrderPaidEvent eventMessage)
    {
        var order = eventMessage.Order;

        try
        {
            var branchText = await _attributes.GetAttributeAsync<string>(order, BranchAttribute, order.StoreId);
            if (string.IsNullOrWhiteSpace(branchText))
                branchText = _configuration["Mercato:DefaultBranchId"];

            if (!Guid.TryParse(branchText, out var branchId) || branchId == Guid.Empty)
                throw new InvalidOperationException($"nopCommerce order {order.Id} has no valid Mercato branch mapping.");

            var customerText = await _attributes.GetAttributeAsync<string>(order, CustomerAttribute, order.StoreId);
            Guid.TryParse(customerText, out var customerId);

            var items = new List<CommerceOrderItem>();
            foreach (var orderItem in await _orders.GetOrderItemsAsync(order.Id))
            {
                var product = await _products.GetProductByIdAsync(orderItem.ProductId)
                    ?? throw new InvalidOperationException($"nopCommerce product {orderItem.ProductId} was not found.");

                if (!TryReadMercatoProductId(product.AdminComment, out var mercatoProductId))
                    throw new InvalidOperationException($"nopCommerce product {product.Id} is not mapped to a Mercato product.");

                items.Add(new CommerceOrderItem(mercatoProductId, orderItem.Quantity));
            }

            var paymentMethod = string.IsNullOrWhiteSpace(order.PaymentMethodSystemName)
                ? "nopCommerce"
                : order.PaymentMethodSystemName;

            await _sync.SyncCompletedOrderAsync(new CommerceOrder(
                order.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                branchId,
                customerId,
                paymentMethod,
                items));
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

    private static bool TryReadMercatoProductId(string? adminComment, out Guid productId)
    {
        productId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(adminComment))
            return false;

        var marker = adminComment.IndexOf(ProductIdPrefix, StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
            return false;

        var value = adminComment[(marker + ProductIdPrefix.Length)..].Split(new[] { '\r', '\n', ';' }, 2)[0].Trim();
        return Guid.TryParse(value, out productId) && productId != Guid.Empty;
    }
}
