using Mercato.Application.DTOs;

namespace Mercato.Application.Services;

public interface IProductService
{
    Task<int> GetProductCountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        CancellationToken cancellationToken = default);
}
