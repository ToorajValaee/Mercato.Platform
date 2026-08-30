using Mercato.Application.DTOs;
using Mercato.Application.Repositories;

namespace Mercato.Application.Services;

public sealed class ProductCatalogServiceImplementation : IProductCatalogService
{
    private readonly IProductRepository _products;
    private readonly IBranchRepository _branches;
    private readonly IInventoryService _inventory;

    public ProductCatalogServiceImplementation(
        IProductRepository products,
        IBranchRepository branches,
        IInventoryService inventory)
    {
        _products = products;
        _branches = branches;
        _inventory = inventory;
    }

    public async Task<IReadOnlyList<CatalogProductDto>> GetCatalogAsync(
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        Guid? normalizedBranchId = branchId is Guid branch && branch != Guid.Empty
            ? branch
            : null;

        if (normalizedBranchId is Guid requestedBranch &&
            await _branches.GetAsync(requestedBranch, cancellationToken) is null)
        {
            throw new InvalidOperationException("Catalog branch was not found.");
        }

        var products = await _products.GetAllAsync(cancellationToken);
        var result = new List<CatalogProductDto>(products.Count);

        foreach (var product in products)
        {
            int? available = null;
            if (normalizedBranchId is Guid selectedBranch)
            {
                available = await _inventory.GetAvailableQuantityAsync(
                    product.Id,
                    selectedBranch,
                    cancellationToken);
            }

            result.Add(new CatalogProductDto(
                product.Id,
                product.Name,
                product.Sku,
                product.ImageUrl,
                product.SalePrice,
                product.CategoryId,
                product.ArtistId,
                normalizedBranchId,
                available));
        }

        return result;
    }
}
