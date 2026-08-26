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

    public PosController(IOrderCheckoutService checkout, IReturnService returns)
    {
        _checkout = checkout;
        _returns = returns;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _checkout.CheckoutAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("returns")]
    public async Task<IActionResult> Return(
        [FromBody] ReturnRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _returns.ReturnAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
