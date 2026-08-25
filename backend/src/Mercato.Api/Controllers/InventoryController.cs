using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("{productId:guid}/{branchId:guid}")]
    public async Task<IActionResult> Get(Guid productId, Guid branchId)
    {
        var quantity = await _inventoryService.GetAvailableQuantityAsync(productId, branchId);
        return Ok(new { productId, branchId, quantity });
    }

    [HttpPost("adjust")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Adjust([FromBody] InventoryAdjustmentRequest request)
    {
        await _inventoryService.AdjustStockAsync(request.ProductId, request.BranchId, request.Quantity, request.Reason);
        return NoContent();
    }

    [HttpPost("transfer")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Transfer([FromBody] InventoryTransferRequest request)
    {
        await _inventoryService.TransferStockAsync(request.ProductId, request.FromBranchId, request.ToBranchId, request.Quantity);
        return NoContent();
    }

    public sealed record InventoryAdjustmentRequest(Guid ProductId, Guid BranchId, decimal Quantity, string Reason);
    public sealed record InventoryTransferRequest(Guid ProductId, Guid FromBranchId, Guid ToBranchId, decimal Quantity);
}
