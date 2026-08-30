using Mercato.Api.Services;
using Mercato.Application.DTOs;
using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/pos")]
[Authorize(Roles = "Admin,Manager,Cashier")]
public sealed class PosController : ControllerBase
{
    private readonly IOrderCheckoutService _checkout;
    private readonly IReturnService _returns;
    private readonly CurrentUserBranchAccess _branchAccess;

    public PosController(IOrderCheckoutService checkout, IReturnService returns, CurrentUserBranchAccess branchAccess)
    {
        _checkout = checkout;
        _returns = returns;
        _branchAccess = branchAccess;
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _returns.GetReturnableOrderAsync(orderId, cancellationToken);
        if (order is null) return NotFound();
        if (!await _branchAccess.CanAccessAsync(order.BranchId, cancellationToken)) return Forbid();
        return Ok(order);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _branchAccess.CanAccessAsync(request.BranchId, cancellationToken)) return Forbid();
            var result = await _checkout.CheckoutAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("returns")]
    public async Task<IActionResult> Return([FromBody] ReturnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _returns.GetReturnableOrderAsync(request.OrderId, cancellationToken);
            if (order is null) return NotFound();
            if (!await _branchAccess.CanAccessAsync(order.BranchId, cancellationToken)) return Forbid();
            var result = await _returns.ReturnAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
