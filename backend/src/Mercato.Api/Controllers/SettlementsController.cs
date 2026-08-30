using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
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
    private readonly ISettlementRepository _repository;
    private readonly IProductRepository _products;

    public SettlementsController(ISettlementService settlements, ISettlementRepository repository, IProductRepository products)
    {
        _settlements = settlements;
        _repository = repository;
        _products = products;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? artistId,
        [FromQuery] bool? isPaid,
        CancellationToken cancellationToken)
    {
        var settlements = await _settlements.GetSettlementsAsync(artistId, isPaid, cancellationToken);
        return Ok(settlements);
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview(
        [FromQuery] Guid artistId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken)
    {
        if (artistId == Guid.Empty || toUtc <= fromUtc)
            return BadRequest(new { error = "Artist and a valid date range are required." });

        var lines = await _repository.GetLinesAsync(artistId, fromUtc, toUtc, cancellationToken);
        var products = await _products.GetAllAsync(cancellationToken);
        return Ok(new
        {
            artistId,
            fromUtc,
            toUtc,
            lineCount = lines.Count,
            quantity = lines.Sum(x => x.QuantitySold),
            totalPurchaseCost = lines.Sum(x => x.PurchaseAmount),
            items = lines.GroupBy(x => x.ProductId).Select(group => new
            {
                productId = group.Key,
                productName = products.FirstOrDefault(x => x.Id == group.Key)?.Name ?? group.Key.ToString(),
                quantity = group.Sum(x => x.QuantitySold),
                purchaseCost = group.Sum(x => x.PurchaseAmount)
            }).OrderBy(x => x.productName)
        });
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
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken cancellationToken)
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
