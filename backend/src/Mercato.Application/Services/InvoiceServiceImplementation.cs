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

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        if (invoice.Id == Guid.Empty)
            invoice.Id = Guid.NewGuid();

        if (invoice.CreatedAt == default)
            invoice.CreatedAt = DateTime.UtcNow;

        return await _invoices.AddAsync(invoice);
    }
}
