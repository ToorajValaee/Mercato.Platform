using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class InvoiceServiceImplementation : IInvoiceService
{
    private readonly IInvoiceRepository _invoices;

    public InvoiceServiceImplementation(IInvoiceRepository invoices)
    {
        _invoices = invoices;
    }

    public async Task<Invoice> CreateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        if (invoice.Id == Guid.Empty)
            invoice.Id = Guid.NewGuid();

        if (invoice.CreatedAt == default)
            invoice.CreatedAt = DateTime.UtcNow;

        if (invoice.OrderId == Guid.Empty || invoice.BranchId == Guid.Empty)
            throw new ArgumentException("Invoice requires an order and branch.");

        if (invoice.TotalAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(invoice.TotalAmount));

        return await _invoices.AddAsync(invoice, cancellationToken);
    }

    public Task<Invoice?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _invoices.GetAsync(id, cancellationToken);

    public Task<IReadOnlyList<Invoice>> GetAllAsync(
        Guid? branchId = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
        => _invoices.GetAllAsync(branchId, customerId, cancellationToken);
}
