namespace Mercato.Application.Services;

public interface IInventoryService
{
    Task<int> GetAvailableQuantityAsync(Guid productId, Guid branchId);
}
