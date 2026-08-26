using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly MercatoDbContext _context;

    public InvoiceRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice> AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public Task<Invoice?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
