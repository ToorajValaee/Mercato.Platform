using Mercato.Application.DTOs;
using Mercato.Application.Repositories;

namespace Mercato.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    public Task<bool> ExistsAsync(Guid productId)
    {
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProductDto> products = Array.Empty<ProductDto>();
        return Task.FromResult(products);
    }
}
