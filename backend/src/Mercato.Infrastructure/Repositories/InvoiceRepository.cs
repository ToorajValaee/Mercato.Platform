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
        => _context.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Invoice>> GetAllAsync(
        Guid? branchId = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices.AsNoTracking().AsQueryable();
        if (branchId is Guid branch && branch != Guid.Empty) query = query.Where(x => x.BranchId == branch);
        if (customerId is Guid customer && customer != Guid.Empty) query = query.Where(x => x.CustomerId == customer);
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }
}
