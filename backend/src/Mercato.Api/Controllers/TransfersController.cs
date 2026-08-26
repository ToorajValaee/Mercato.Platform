using Mercato.Application.DTOs;
using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize(Roles = "Admin,Manager")]
public sealed class TransfersController : ControllerBase
{
    private readonly IBranchTransferService _transfers;

    public TransfersController(IBranchTransferService transfers)
    {
        _transfers = transfers;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => Ok(await _transfers.GetAllAsync(branchId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBranchTransferRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var transfer = await _transfers.CreateAsync(request, cancellationToken);
            return Ok(transfer);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
