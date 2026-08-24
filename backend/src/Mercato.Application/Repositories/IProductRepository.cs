namespace Mercato.Application.Repositories;

public interface IProductRepository
{
    Task<bool> ExistsAsync(Guid productId);
}
