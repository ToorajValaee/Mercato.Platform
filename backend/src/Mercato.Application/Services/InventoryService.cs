namespace Mercato.Application.Services;

public class InventoryService : IInventoryService
{
    public Task<int> GetAvailableQuantityAsync(Guid productId, Guid branchId)
    {
        return Task.FromResult(0);
    }
}
