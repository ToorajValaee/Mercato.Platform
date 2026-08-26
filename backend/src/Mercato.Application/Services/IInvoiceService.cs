using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public interface IInvoiceService
{
    Task<Invoice> CreateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<Invoice?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetAllAsync(Guid? branchId = null, Guid? customerId = null, CancellationToken cancellationToken = default);
}
