using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IPaymentRepository
{
    Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
