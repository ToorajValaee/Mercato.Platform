namespace Mercato.Application.Services;

public interface IStockQueryService
{
    Task<decimal> GetStockAsync(Guid productId, Guid branchId, CancellationToken cancellationToken = default);
}
