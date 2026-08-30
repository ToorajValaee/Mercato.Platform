using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly MercatoDbContext _context;

    public UserRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Users.AsNoTracking().OrderBy(x => x.Email).ToListAsync(cancellationToken);

    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return false;
        var assignments = _context.UserBranchAssignments.Where(x => x.UserId == id);
        _context.UserBranchAssignments.RemoveRange(assignments);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Guid>> GetBranchIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.UserBranchAssignments.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.BranchId)
            .Select(x => x.BranchId)
            .ToListAsync(cancellationToken);

    public async Task SetBranchIdsAsync(Guid userId, IReadOnlyCollection<Guid> branchIds, CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserBranchAssignments
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
        _context.UserBranchAssignments.RemoveRange(existing);
        foreach (var branchId in branchIds.Distinct())
            _context.UserBranchAssignments.Add(new UserBranchAssignment { UserId = userId, BranchId = branchId });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
