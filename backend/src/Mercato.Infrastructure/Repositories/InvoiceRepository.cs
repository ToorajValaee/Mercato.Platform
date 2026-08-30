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
        return _context.Invoices.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _context.Invoices.AsNoTracking().Include(x => x.Items).FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetAllAsync(Guid? branchId = null, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices.AsNoTracking().AsQueryable();
        if (branchId.HasValue && branchId.Value != Guid.Empty)
            query = query.Where(x => x.BranchId == branchId.Value);
        if (customerId.HasValue && customerId.Value != Guid.Empty)
            query = query.Where(x => x.CustomerId == customerId.Value);
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }
}
