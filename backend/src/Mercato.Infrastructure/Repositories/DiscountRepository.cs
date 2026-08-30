using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Infrastructure.Repositories;

public sealed class DiscountRepository : IDiscountRepository
{
    private readonly MercatoDbContext _context;
    public DiscountRepository(MercatoDbContext context) => _context = context;

    public async Task<IReadOnlyList<DiscountDefinition>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.DiscountDefinitions.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<DiscountDefinition?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.DiscountDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<DiscountDefinition> AddAsync(DiscountDefinition discount, CancellationToken cancellationToken = default)
    {
        _context.DiscountDefinitions.Add(discount);
        await _context.SaveChangesAsync(cancellationToken);
        return discount;
    }

    public async Task<DiscountDefinition?> UpdateAsync(DiscountDefinition discount, CancellationToken cancellationToken = default)
    {
        var existing = await _context.DiscountDefinitions.FirstOrDefaultAsync(x => x.Id == discount.Id, cancellationToken);
        if (existing is null) return null;
        existing.Name = discount.Name;
        existing.Type = discount.Type;
        existing.Value = discount.Value;
        existing.IsActive = discount.IsActive;
        existing.SortOrder = discount.SortOrder;
        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _context.DiscountDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing is null) return false;
        _context.DiscountDefinitions.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
