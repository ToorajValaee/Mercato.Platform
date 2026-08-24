namespace Mercato.Application.Repositories;

public interface IInventoryRepository
{
    Task<int> GetAvailableQuantityAsync(Guid branchId, Guid productId);
    Task AddMovementAsync(Guid branchId, Guid productId, int quantity, string movementType);
}
