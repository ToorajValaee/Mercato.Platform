namespace Mercato.Application.Repositories;

using Mercato.Application.DTOs;

public interface IProductRepository
{
    Task<bool> ExistsAsync(Guid productId);

    Task<IReadOnlyList<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
