using Mercato.Application.DTOs;

namespace Mercato.Application.Services;

public sealed class OrderCheckoutServiceImplementation : IOrderCheckoutService
{
    private readonly IOrderService _orders;

    public OrderCheckoutServiceImplementation(IOrderService orders)
    {
        _orders = orders;
    }

    public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
            throw new InvalidOperationException("Checkout requires items.");

        var total = request.Items.Sum(x => x.Quantity);

        var order = await _orders.CreateAsync(new Mercato.Domain.Entities.Order
        {
            BranchId = request.BranchId,
            TotalAmount = total
        }, cancellationToken);

        return new CheckoutResult
        {
            OrderId = order.Id,
            Total = total,
            Status = "Created"
        };
    }
}
