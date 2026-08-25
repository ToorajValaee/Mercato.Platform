using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class InvoiceServiceImplementation : IInvoiceService
{
    public Task<Invoice> CreateAsync(Invoice invoice)
    {
        invoice.Id = Guid.NewGuid();
        invoice.CreatedAt = DateTime.UtcNow;
        return Task.FromResult(invoice);
    }
}
