using Mercato.Application.DTOs;
using Mercato.Application.Repositories;

namespace Mercato.Application.Services;

public sealed class ProductCatalogServiceImplementation : IProductCatalogService
{
    private readonly IProductRepository _products;
    private readonly IInventoryService _inventory;

    public ProductCatalogServiceImplementation(IProductRepository products, IInventoryService inventory)
    {
        _products = products;
        _inventory = inventory;
    }

    public async Task<IReadOnlyList<CatalogProductDto>> GetCatalogAsync(
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var products = await _products.GetAllAsync(cancellationToken);
        var result = new List<CatalogProductDto>(products.Count);

        foreach (var product in products)
        {
            int? available = null;
            if (branchId is Guid branch && branch != Guid.Empty)
                available = await _inventory.GetAvailableQuantityAsync(product.Id, branch);

            result.Add(new CatalogProductDto(
                product.Id,
                product.Name,
                product.Sku,
                product.SalePrice,
                product.CategoryId,
                product.ArtistId,
                branchId,
                available));
        }

        return result;
    }
}
