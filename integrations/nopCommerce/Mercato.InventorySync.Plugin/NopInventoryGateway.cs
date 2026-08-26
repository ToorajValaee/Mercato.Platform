using Mercato.NopCommerce.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

namespace Mercato.InventorySync.Plugin;

public sealed class NopInventoryGateway : INopInventoryGateway
{
    private readonly IProductService _products;

    public NopInventoryGateway(IProductService products)
    {
        _products = products;
    }

    public async Task SetStockAsync(CatalogProduct product, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 0) quantity = 0;

        var entity = await _products.GetProductBySkuAsync(product.NopSku);
        if (entity is null)
            return;

        entity.ManageInventoryMethodId = (int)ManageInventoryMethod.ManageStock;
        entity.StockQuantity = quantity;
        entity.DisplayStockAvailability = true;
        entity.UpdatedOnUtc = DateTime.UtcNow;
        await _products.UpdateProductAsync(entity);
    }
}
