using Mercato.Application.DTOs;

namespace Mercato.Application.Services;

public sealed class ProductServiceImplementation : IProductService
{
    public string ServiceName => "Product Management Service";

    public Task<int> GetProductCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProductDto> products = Array.Empty<ProductDto>();
        return Task.FromResult(products);
    }
}
