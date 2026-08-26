using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/catalog")]
[Authorize]
public sealed class CatalogController : ControllerBase
{
    private readonly IProductCatalogService _catalog;

    public CatalogController(IProductCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => Ok(await _catalog.GetCatalogAsync(branchId, cancellationToken));
}
