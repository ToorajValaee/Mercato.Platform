using Mercato.Application.DTOs;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class OrderCheckoutServiceImplementation : IOrderCheckoutService
{
    private readonly IOrderService _orders;
    private readonly IInvoiceService _invoices;

    public OrderCheckoutServiceImplementation(IOrderService orders, IInvoiceService invoices)
    {
        _orders = orders;
        _invoices = invoices;
    }

    public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
            throw new InvalidOperationException("Checkout requires items.");

        var total = request.Items.Sum(x => x.Quantity * x.UnitPrice);

        var order = await _orders.CreateAsync(new Order
        {
            BranchId = request.BranchId,
            TotalAmount = total
        }, cancellationToken);

        await _invoices.CreateAsync(new Invoice
        {
            CustomerId = request.CustomerId,
            BranchId = request.BranchId,
            TotalAmount = total
        });

        return new CheckoutResult(true, $"Order {order.Id} created.");
    }
}
