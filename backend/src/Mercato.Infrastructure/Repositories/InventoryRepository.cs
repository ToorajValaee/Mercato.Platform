namespace Mercato.Infrastructure.Repositories;

public class InventoryRepository
{
    public Task<int> GetAvailableQuantityAsync(Guid productId, Guid branchId)
    {
        return Task.FromResult(0);
    }
}
