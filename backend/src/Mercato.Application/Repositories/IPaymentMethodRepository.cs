using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IPaymentMethodRepository
{
    Task<IReadOnlyList<PaymentMethod>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<PaymentMethod?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentMethod> AddAsync(PaymentMethod method, CancellationToken cancellationToken = default);
    Task<PaymentMethod?> UpdateAsync(PaymentMethod method, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
