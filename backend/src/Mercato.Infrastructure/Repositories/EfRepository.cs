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

    public async Task<T?> GetAsync(Guid id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
    }

    public void Remove(T entity)
    {
        _context.Set<T>().Remove(entity);
    }
}
