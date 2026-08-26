using Mercato.NopCommerce.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

namespace Mercato.ProductSync.Plugin;

public sealed class NopProductGateway : INopProductGateway
{
    public const string MercatoProductIdPrefix = "Mercato.ProductId=";

    private readonly IProductService _products;

    public NopProductGateway(IProductService products)
    {
        _products = products;
    }

    public async Task UpsertAsync(CatalogProduct product, CancellationToken cancellationToken = default)
    {
        var sku = product.NopSku;
        var entity = await _products.GetProductBySkuAsync(sku);
        var now = DateTime.UtcNow;
        var identity = $"{MercatoProductIdPrefix}{product.ProductId:D}";

        if (entity is null)
        {
            entity = new Product
            {
                ProductTypeId = (int)ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = product.Name,
                Sku = sku,
                AdminComment = identity,
                Price = product.SalePrice,
                ManageInventoryMethodId = (int)ManageInventoryMethod.ManageStock,
                Published = true,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                CreatedOnUtc = now,
                UpdatedOnUtc = now
            };

            await _products.InsertProductAsync(entity);
            return;
        }

        entity.Name = product.Name;
        entity.AdminComment = identity;
        entity.Price = product.SalePrice;
        entity.Published = true;
        entity.UpdatedOnUtc = now;
        await _products.UpdateProductAsync(entity);
    }
}
