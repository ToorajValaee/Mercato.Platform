using Mercato.Application.Interfaces;

namespace Mercato.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync()
    {
        return Task.FromResult(0);
    }
}
