using Mercato.Application.DTOs;

namespace Mercato.Application.Services;

public interface IProductCatalogService
{
    Task<IReadOnlyList<CatalogProductDto>> GetCatalogAsync(
        Guid? branchId = null,
        CancellationToken cancellationToken = default);
}
