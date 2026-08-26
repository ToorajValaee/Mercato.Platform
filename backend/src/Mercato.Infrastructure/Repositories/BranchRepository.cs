using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class BranchRepository : IBranchRepository
{
    private readonly MercatoDbContext _context;

    public BranchRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Branches.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<Branch?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Branch> AddAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        _context.Branches.Add(branch);
        await _context.SaveChangesAsync(cancellationToken);
        return branch;
    }

    public async Task<Branch?> UpdateAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        if (!await _context.Branches.AnyAsync(x => x.Id == branch.Id, cancellationToken)) return null;
        _context.Branches.Update(branch);
        await _context.SaveChangesAsync(cancellationToken);
        return branch;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (branch is null) return false;
        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
