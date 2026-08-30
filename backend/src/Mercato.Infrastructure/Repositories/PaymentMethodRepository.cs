using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Infrastructure.Repositories;

public sealed class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly MercatoDbContext _context;
    public PaymentMethodRepository(MercatoDbContext context) => _context = context;

    public async Task<IReadOnlyList<PaymentMethod>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.PaymentMethods.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<PaymentMethod?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.PaymentMethods.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PaymentMethod> AddAsync(PaymentMethod method, CancellationToken cancellationToken = default)
    {
        _context.PaymentMethods.Add(method);
        await _context.SaveChangesAsync(cancellationToken);
        return method;
    }

    public async Task<PaymentMethod?> UpdateAsync(PaymentMethod method, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PaymentMethods.FirstOrDefaultAsync(x => x.Id == method.Id, cancellationToken);
        if (existing is null) return null;
        existing.Name = method.Name;
        existing.IsActive = method.IsActive;
        existing.SortOrder = method.SortOrder;
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PaymentMethods.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing is null) return false;
        _context.PaymentMethods.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
