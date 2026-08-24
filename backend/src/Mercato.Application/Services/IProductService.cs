namespace Mercato.Application.Services;

public interface IProductService
{
    Task<int> GetProductCountAsync(CancellationToken cancellationToken = default);
}
