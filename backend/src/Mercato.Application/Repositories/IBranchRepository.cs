using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IBranchRepository
{
    Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Branch?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Branch> AddAsync(Branch branch, CancellationToken cancellationToken = default);
    Task<Branch?> UpdateAsync(Branch branch, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
