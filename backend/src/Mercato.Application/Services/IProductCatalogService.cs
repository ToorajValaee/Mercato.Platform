namespace Mercato.Application.Services;

public interface IProductCatalogService
{
    Task<IEnumerable<object>> GetCatalogAsync(CancellationToken cancellationToken = default);
}
