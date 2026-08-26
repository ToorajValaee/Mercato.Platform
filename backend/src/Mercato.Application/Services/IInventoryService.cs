using Mercato.Application.DTOs;

namespace Mercato.Application.Services;

public interface IInventoryService
{
    Task<int> GetAvailableQuantityAsync(Guid productId, Guid branchId, CancellationToken cancellationToken = default);

    Task AdjustStockAsync(
        Guid productId,
        Guid branchId,
        decimal quantity,
        string reason,
        CancellationToken cancellationToken = default);

    Task TransferStockAsync(
        Guid productId,
        Guid fromBranchId,
        Guid toBranchId,
        decimal quantity,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(
        Guid? branchId = null,
        Guid? productId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);
}
