using Mercato.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "Admin,Manager")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoices;

    public InvoicesController(IInvoiceService invoices)
    {
        _invoices = invoices;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? customerId,
        CancellationToken cancellationToken)
        => Ok(await _invoices.GetAllAsync(branchId, customerId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _invoices.GetAsync(id, cancellationToken);
        return invoice is null ? NotFound() : Ok(invoice);
    }
}
