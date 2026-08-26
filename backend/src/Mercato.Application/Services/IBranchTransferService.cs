using Mercato.Application.DTOs;

namespace Mercato.Application.Services;

public interface IBranchTransferService
{
    Task<BranchTransferDto> CreateAsync(CreateBranchTransferRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BranchTransferDto>> GetAllAsync(Guid? branchId = null, CancellationToken cancellationToken = default);
}
