using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<Invoice?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetAllAsync(
        Guid? branchId = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default);
}
