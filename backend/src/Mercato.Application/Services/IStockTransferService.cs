namespace Mercato.Application.Services;

public interface IStockTransferService
{
    Task TransferAsync(Guid sourceBranchId, Guid destinationBranchId, Guid productId, int quantity);
}
