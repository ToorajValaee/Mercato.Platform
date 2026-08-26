using Mercato.Application.DTOs;
using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/settlements")]
[Authorize(Roles = "Admin,Manager")]
public sealed class SettlementsController : ControllerBase
{
    private readonly ISettlementService _settlements;

    public SettlementsController(ISettlementService settlements)
    {
        _settlements = settlements;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? artistId,
        [FromQuery] bool? isPaid,
        CancellationToken cancellationToken)
    {
        var settlements = await _settlements.GetSettlementsAsync(
            artistId,
            isPaid,
            cancellationToken);

        return Ok(settlements);
    }

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(
        [FromBody] SettlementCalculationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var settlement = await _settlements.CalculateAsync(
                request.ArtistId,
                request.PeriodFromUtc,
                request.PeriodToUtc,
                cancellationToken);

            return Ok(settlement);
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

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var settlement = await _settlements.MarkPaidAsync(id, cancellationToken);
            return settlement is null ? NotFound() : Ok(settlement);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
