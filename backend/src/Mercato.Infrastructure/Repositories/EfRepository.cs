using Microsoft.EntityFrameworkCore;
using Mercato.Application.Interfaces;

namespace Mercato.Infrastructure.Repositories;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;

    public EfRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _context.Set<T>().AddAsync(entity, cancellationToken);
    }

    public void Remove(T entity)
    {
        _context.Set<T>().Remove(entity);
    }
}
