using Mercato.NopCommerce.Core;

namespace Mercato.InventorySync.Plugin;

public interface INopInventoryGateway
{
    Task SetStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}

public sealed class InventorySyncCore
{
    private readonly MercatoApiClient _mercato;
    private readonly INopInventoryGateway _nop;

    public InventorySyncCore(MercatoApiClient mercato, INopInventoryGateway nop)
    {
        _mercato = mercato;
        _nop = nop;
    }

    public async Task<int> SyncBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty) throw new ArgumentException("Branch is required.", nameof(branchId));
        var products = await _mercato.GetCatalogAsync(branchId, cancellationToken);
        foreach (var product in products)
            await _nop.SetStockAsync(product.ProductId, product.AvailableQuantity ?? 0, cancellationToken);
        return products.Count;
    }
}
