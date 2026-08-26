using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IBranchTransferRepository
{
    Task<BranchTransfer> AddAsync(BranchTransfer transfer, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BranchTransfer>> GetAllAsync(Guid? branchId = null, CancellationToken cancellationToken = default);
}
