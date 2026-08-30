using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IDiscountRepository
{
    Task<IReadOnlyList<DiscountDefinition>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<DiscountDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DiscountDefinition> AddAsync(DiscountDefinition discount, CancellationToken cancellationToken = default);
    Task<DiscountDefinition?> UpdateAsync(DiscountDefinition discount, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
