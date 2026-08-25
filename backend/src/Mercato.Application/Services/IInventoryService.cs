namespace Mercato.Application.Services;

public interface IInventoryService
{
    Task<int> GetAvailableQuantityAsync(Guid productId, Guid branchId);

    Task AdjustStockAsync(
        Guid productId,
        Guid branchId,
        decimal quantity,
        string reason);

    Task TransferStockAsync(
        Guid productId,
        Guid fromBranchId,
        Guid toBranchId,
        decimal quantity);
}
