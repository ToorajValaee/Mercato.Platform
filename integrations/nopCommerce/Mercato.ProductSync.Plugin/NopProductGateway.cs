using Mercato.NopCommerce.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Common;

namespace Mercato.ProductSync.Plugin;

public sealed class NopProductGateway : INopProductGateway
{
    private readonly IProductService _products;
    private readonly IGenericAttributeService _attributes;

    public NopProductGateway(
        IProductService products,
        IGenericAttributeService attributes)
    {
        _products = products;
        _attributes = attributes;
    }

    public async Task UpsertAsync(CatalogProduct product, CancellationToken cancellationToken = default)
    {
        var sku = product.NopSku;
        var entity = await _products.GetProductBySkuAsync(sku);
        var now = DateTime.UtcNow;

        if (entity is null)
        {
            entity = new Product
            {
                ProductTypeId = (int)ProductType.SimpleProduct,
                VisibleIndividually = true,
                Name = product.Name,
                Sku = sku,
                Price = product.SalePrice,
                ManageInventoryMethodId = (int)ManageInventoryMethod.ManageStock,
                Published = true,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000,
                CreatedOnUtc = now,
                UpdatedOnUtc = now
            };

            await _products.InsertProductAsync(entity);
            await _attributes.SaveAttributeAsync(entity, MercatoNopDefaults.ProductIdAttribute, product.ProductId.ToString("D"));
            return;
        }

        entity.Name = product.Name;
        entity.Price = product.SalePrice;
        entity.Published = true;
        entity.UpdatedOnUtc = now;
        await _products.UpdateProductAsync(entity);
        await _attributes.SaveAttributeAsync(entity, MercatoNopDefaults.ProductIdAttribute, product.ProductId.ToString("D"));
    }
}
