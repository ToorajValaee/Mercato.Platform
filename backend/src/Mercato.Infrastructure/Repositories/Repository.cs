namespace Mercato.Infrastructure.Repositories;

public class Repository<T> where T : class
{
    public virtual Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<T?>(null);
    }
}
