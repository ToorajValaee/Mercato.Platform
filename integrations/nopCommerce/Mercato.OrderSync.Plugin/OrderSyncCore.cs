using System.Text.Json;
using Mercato.NopCommerce.Core;

namespace Mercato.OrderSync.Plugin;

public sealed class OrderSyncCore
{
    private readonly MercatoApiClient _mercato;

    public OrderSyncCore(MercatoApiClient mercato)
    {
        _mercato = mercato;
    }

    public Task<JsonElement> SyncCompletedOrderAsync(CommerceOrder order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(order.ExternalOrderId)) throw new ArgumentException("Order is required.", nameof(order));
        if (order.BranchId == Guid.Empty) throw new ArgumentException("Branch is required.", nameof(order));
        if (string.IsNullOrWhiteSpace(order.PaymentMethod)) throw new ArgumentException("Payment method is required.", nameof(order));
        if (order.Items.Count == 0) throw new InvalidOperationException("Completed order contains no items.");
        return _mercato.SyncOrderAsync(order, cancellationToken);
    }
}
