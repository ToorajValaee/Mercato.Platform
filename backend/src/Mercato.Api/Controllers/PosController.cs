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

    public PosController(IOrderCheckoutService checkout)
    {
        _checkout = checkout;
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
