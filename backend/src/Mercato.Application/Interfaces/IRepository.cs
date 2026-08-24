namespace Mercato.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
}
