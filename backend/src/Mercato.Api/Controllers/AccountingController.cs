using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/accounting")]
[Authorize(Roles = "Admin,Manager")]
public sealed class AccountingController : ControllerBase
{
    private readonly IAccountingService _accounting;

    public AccountingController(IAccountingService accounting)
    {
        _accounting = accounting;
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions(
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
        => Ok(await _accounting.GetTransactionsAsync(branchId, fromUtc, toUtc, type, cancellationToken));

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _accounting.GetSummaryAsync(branchId, fromUtc, toUtc, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
