using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface ISalesReturnRepository
{
    Task<SalesReturn> AddAsync(SalesReturn salesReturn, CancellationToken cancellationToken = default);
    Task<int> GetReturnedQuantityAsync(
        Guid orderId,
        Guid productId,
        CancellationToken cancellationToken = default,
        bool serialize = false);
}
