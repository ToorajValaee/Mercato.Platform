using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IInventoryRepository
{
    Task<int> GetAvailableQuantityAsync(Guid branchId, Guid productId, CancellationToken cancellationToken = default);
    Task AddMovementAsync(Guid branchId, Guid productId, int quantity, string movementType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        Guid? branchId = null,
        Guid? productId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);
}
