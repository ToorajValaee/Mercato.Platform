namespace Mercato.Infrastructure.Repositories;

public class ProductRepository
{
    public Task<object?> GetByIdAsync(Guid id)
    {
        return Task.FromResult<object?>(null);
    }
}
