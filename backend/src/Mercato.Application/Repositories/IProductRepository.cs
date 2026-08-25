namespace Mercato.Application.Repositories;

using Mercato.Application.DTOs;

public interface IProductRepository
{
    Task<bool> ExistsAsync(Guid productId);

    Task<IReadOnlyList<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ProductDto> AddAsync(ProductDto product, CancellationToken cancellationToken = default);

    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
