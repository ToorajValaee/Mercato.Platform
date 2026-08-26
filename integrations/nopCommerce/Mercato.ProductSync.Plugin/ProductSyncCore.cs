using Mercato.NopCommerce.Core;

namespace Mercato.ProductSync.Plugin;

public interface INopProductGateway
{
    Task UpsertAsync(CatalogProduct product, CancellationToken cancellationToken = default);
}

public sealed class ProductSyncCore
{
    private readonly MercatoApiClient _mercato;
    private readonly INopProductGateway _nop;

    public ProductSyncCore(MercatoApiClient mercato, INopProductGateway nop)
    {
        _mercato = mercato;
        _nop = nop;
    }

    public async Task<int> SyncAsync(CancellationToken cancellationToken = default)
    {
        var products = await _mercato.GetCatalogAsync(null, cancellationToken);
        foreach (var product in products)
            await _nop.UpsertAsync(product, cancellationToken);
        return products.Count;
    }
}
