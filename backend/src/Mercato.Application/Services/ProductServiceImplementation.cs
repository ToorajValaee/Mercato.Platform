using Mercato.Application.DTOs;
using Mercato.Application.Repositories;

namespace Mercato.Application.Services;

public sealed class ProductServiceImplementation : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductServiceImplementation(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public string ServiceName => "Product Management Service";

    public async Task<int> GetProductCountAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.Count;
    }

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return _productRepository.GetAllAsync(cancellationToken);
    }

    public Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new ProductDto(
            Guid.NewGuid(),
            request.Name,
            request.PurchasePrice,
            request.SalePrice,
            request.CategoryId);

        return _productRepository.AddAsync(product, cancellationToken);
    }

    public Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        return _productRepository.UpdateAsync(id, request, cancellationToken);
    }

    public Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _productRepository.ArchiveAsync(id, cancellationToken);
    }
}
