namespace Mercato.Application.Services;

public interface ICategoryService
{
    Task<IEnumerable<object>> GetAllAsync(CancellationToken cancellationToken = default);
}
