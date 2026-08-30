using Mercato.Application.Repositories;
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
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ICustomerRepository _customers;
    private readonly IBranchRepository _branches;
    private readonly IPaymentRepository _payments;

    public InvoicesController(
        IInvoiceService invoices,
        IOrderRepository orders,
        IProductRepository products,
        ICustomerRepository customers,
        IBranchRepository branches,
        IPaymentRepository payments)
    {
        _invoices = invoices;
        _orders = orders;
        _products = products;
        _customers = customers;
        _branches = branches;
        _payments = payments;
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
        if (invoice is null) return NotFound();

        var order = await _orders.GetAsync(invoice.OrderId, cancellationToken);
        var products = await _products.GetAllAsync(cancellationToken);
        var branch = await _branches.GetAsync(invoice.BranchId, cancellationToken);
        var customer = invoice.CustomerId == Guid.Empty ? null : await _customers.GetAsync(invoice.CustomerId, cancellationToken);
        var payment = await _payments.GetSalePaymentByOrderIdAsync(invoice.OrderId, cancellationToken);

        var persistedLines = invoice.Items.Select(x => new { x.ProductId, x.Quantity, x.UnitPrice }).ToList();
        var lines = persistedLines.Count > 0
            ? persistedLines
            : order?.Items.Select(x => new { x.ProductId, Quantity = (decimal)x.Quantity, x.UnitPrice }).ToList() ?? [];

        return Ok(new
        {
            invoice.Id,
            invoice.OrderId,
            invoice.BranchId,
            branchName = branch?.Name,
            invoice.CustomerId,
            customerName = customer?.Name ?? "Guest",
            customerPhone = customer?.Phone,
            createdAtUtc = invoice.CreatedAt,
            subtotal = invoice.SubtotalAmount == 0 ? invoice.TotalAmount : invoice.SubtotalAmount,
            invoice.DiscountName,
            invoice.DiscountAmount,
            total = invoice.TotalAmount,
            paymentMethod = payment?.Method,
            paymentReference = payment?.Reference,
            paidAtUtc = payment?.PaidAt,
            items = lines.Select(line => new
            {
                line.ProductId,
                productName = products.FirstOrDefault(x => x.Id == line.ProductId)?.Name ?? line.ProductId.ToString(),
                line.Quantity,
                line.UnitPrice,
                lineTotal = line.Quantity * line.UnitPrice
            })
        });
    }
}
