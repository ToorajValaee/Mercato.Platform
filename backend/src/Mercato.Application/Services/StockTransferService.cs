namespace Mercato.Application.Services;

public class StockTransferService : IStockTransferService
{
    public Task TransferAsync(Guid sourceBranchId, Guid destinationBranchId, Guid productId, decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive");

        return Task.CompletedTask;
    }
}
