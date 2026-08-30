using Mercato.Api.Services;
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
    private readonly CurrentUserBranchAccess _branchAccess;

    public InventoryController(IInventoryService inventory, IBranchTransferService transfers, CurrentUserBranchAccess branchAccess)
    {
        _inventory = inventory;
        _transfers = transfers;
        _branchAccess = branchAccess;
    }

    [HttpGet("{productId:guid}/{branchId:guid}")]
    public async Task<IActionResult> Get(Guid productId, Guid branchId, CancellationToken cancellationToken)
    {
        if (!await _branchAccess.CanAccessAsync(branchId, cancellationToken)) return Forbid();
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
            if (branchId.HasValue && !await _branchAccess.CanAccessAsync(branchId.Value, cancellationToken)) return Forbid();
            var movements = await _inventory.GetMovementsAsync(branchId, productId, fromUtc, toUtc, cancellationToken);
            if (_branchAccess.IsAdmin || branchId.HasValue) return Ok(movements);
            var allowed = (await _branchAccess.GetAllowedBranchIdsAsync(cancellationToken)).ToHashSet();
            return Ok(movements.Where(x => allowed.Contains(x.BranchId)));
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
        if (!await _branchAccess.CanAccessAsync(request.BranchId, cancellationToken)) return Forbid();
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
        if (!await _branchAccess.CanAccessAsync(request.SourceBranchId, cancellationToken) ||
            !await _branchAccess.CanAccessAsync(request.DestinationBranchId, cancellationToken))
            return Forbid();
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
