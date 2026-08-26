using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface ICheckoutIdempotencyRepository
{
    Task<CheckoutIdempotencyRecord?> GetAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CheckoutIdempotencyRecord record,
        CancellationToken cancellationToken = default);
}
