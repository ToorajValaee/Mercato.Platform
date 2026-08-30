using Mercato.Api.Services;
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
    private readonly CurrentUserBranchAccess _branchAccess;

    public CatalogController(IProductCatalogService catalog, CurrentUserBranchAccess branchAccess)
    {
        _catalog = catalog;
        _branchAccess = branchAccess;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? branchId, CancellationToken cancellationToken)
    {
        try
        {
            if (branchId.HasValue && !await _branchAccess.CanAccessAsync(branchId.Value, cancellationToken))
                return Forbid();
            return Ok(await _catalog.GetCatalogAsync(branchId, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
