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
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
