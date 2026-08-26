using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly MercatoDbContext _context;

    public CategoryRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<Category?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<Category?> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        if (!await _context.Categories.AnyAsync(x => x.Id == category.Id, cancellationToken)) return null;
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null) return false;
        var hasChildren = await _context.Categories.AnyAsync(x => x.ParentId == id, cancellationToken);
        var hasProducts = await _context.Products.AnyAsync(x => x.CategoryId == id, cancellationToken);
        if (hasChildren || hasProducts)
            throw new InvalidOperationException("Category cannot be deleted while it has child categories or products.");
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
