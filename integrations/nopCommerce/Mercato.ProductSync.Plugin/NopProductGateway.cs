using Mercato.NopCommerce.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Common;

namespace Mercato.ProductSync.Plugin;

public sealed class NopProductGateway : INopProductGateway
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IGenericAttributeService _attributes;

    public NopProductGateway(
        IProductService products,
        ICategoryService categories,
        IGenericAttributeService attributes)
    {
        _products = products;
        _categories = categories;
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
        }
        else
        {
            entity.Name = product.Name;
            entity.Price = product.SalePrice;
            entity.Published = true;
            entity.UpdatedOnUtc = now;
            await _products.UpdateProductAsync(entity);
        }

        await _attributes.SaveAttributeAsync(entity, MercatoNopDefaults.ProductIdAttribute, product.ProductId.ToString("D"));
        await SynchronizeCategoryAsync(entity, product, now);
    }

    private async Task SynchronizeCategoryAsync(Product product, CatalogProduct source, DateTime now)
    {
        Category? targetCategory = null;
        if (source.CategoryId is Guid categoryId && categoryId != Guid.Empty)
        {
            var mercatoCategoryId = categoryId.ToString("D");
            var allCategories = await _categories.GetAllCategoriesAsync(showHidden: true);

            foreach (var category in allCategories)
            {
                var mappedId = await _attributes.GetAttributeAsync<string>(
                    category,
                    MercatoNopDefaults.CategoryIdAttribute);
                if (string.Equals(mappedId, mercatoCategoryId, StringComparison.OrdinalIgnoreCase))
                {
                    targetCategory = category;
                    break;
                }
            }

            if (targetCategory is null)
            {
                targetCategory = new Category
                {
                    Name = string.IsNullOrWhiteSpace(source.CategoryName) ? mercatoCategoryId : source.CategoryName.Trim(),
                    Published = true,
                    DisplayOrder = 0,
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now
                };
                await _categories.InsertCategoryAsync(targetCategory);
                await _attributes.SaveAttributeAsync(targetCategory, MercatoNopDefaults.CategoryIdAttribute, mercatoCategoryId);
            }
            else
            {
                var categoryName = source.CategoryName?.Trim();
                if (!string.IsNullOrWhiteSpace(categoryName) &&
                    !string.Equals(targetCategory.Name, categoryName, StringComparison.Ordinal))
                {
                    targetCategory.Name = categoryName;
                    targetCategory.Published = true;
                    targetCategory.UpdatedOnUtc = now;
                    await _categories.UpdateCategoryAsync(targetCategory);
                }
            }
        }

        var mappings = await _categories.GetProductCategoriesByProductIdAsync(product.Id, showHidden: true);
        foreach (var mapping in mappings.ToList())
        {
            var mappedCategory = await _categories.GetCategoryByIdAsync(mapping.CategoryId);
            if (mappedCategory is null)
                continue;

            var mappedMercatoId = await _attributes.GetAttributeAsync<string>(
                mappedCategory,
                MercatoNopDefaults.CategoryIdAttribute);
            if (string.IsNullOrWhiteSpace(mappedMercatoId))
                continue;

            if (targetCategory is null || mapping.CategoryId != targetCategory.Id)
                await _categories.DeleteProductCategoryAsync(mapping);
        }

        if (targetCategory is null)
            return;

        var refreshedMappings = await _categories.GetProductCategoriesByProductIdAsync(product.Id, showHidden: true);
        if (refreshedMappings.Any(mapping => mapping.CategoryId == targetCategory.Id))
            return;

        await _categories.InsertProductCategoryAsync(new ProductCategory
        {
            ProductId = product.Id,
            CategoryId = targetCategory.Id,
            DisplayOrder = 0
        });
    }
}
