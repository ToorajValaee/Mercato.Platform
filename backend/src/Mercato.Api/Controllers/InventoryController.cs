using Mercato.Application.DTOs;
using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public sealed class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;
    private readonly IBranchTransferService _transfers;

    public InventoryController(IInventoryService inventory, IBranchTransferService transfers)
    {
        _inventory = inventory;
        _transfers = transfers;
    }

    [HttpGet("{productId:guid}/{branchId:guid}")]
    public async Task<IActionResult> Get(Guid productId, Guid branchId, CancellationToken cancellationToken)
    {
        try
        {
            var quantity = await _inventory.GetAvailableQuantityAsync(productId, branchId, cancellationToken);
            return Ok(new { productId, branchId, quantity });
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpGet("movements")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetMovements(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? productId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _inventory.GetMovementsAsync(branchId, productId, fromUtc, toUtc, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Adjust([FromBody] InventoryAdjustmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _inventory.AdjustStockAsync(request.ProductId, request.BranchId, request.Quantity, request.Reason, cancellationToken);
            return NoContent();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("transfer")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Transfer([FromBody] CreateBranchTransferRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _transfers.CreateAsync(request, cancellationToken));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
