using Mercato.Application.DTOs;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class OrderCheckoutServiceImplementation : IOrderCheckoutService
{
    private readonly IOrderService _orders;
    private readonly IInvoiceService _invoices;
    private readonly IInventoryService _inventory;
    private readonly ISettlementService _settlements;

    public OrderCheckoutServiceImplementation(
        IOrderService orders,
        IInvoiceService invoices,
        IInventoryService inventory,
        ISettlementService settlements)
    {
        _orders = orders;
        _invoices = invoices;
        _inventory = inventory;
        _settlements = settlements;
    }

    public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
            throw new InvalidOperationException("Checkout requires items.");

        foreach (var item in request.Items)
        {
            var available = await _inventory.GetAvailableQuantityAsync(item.ProductId, request.BranchId);
            if (available < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}.");
        }

        var total = request.Items.Sum(x => x.Quantity * x.UnitPrice);

        var order = await _orders.CreateAsync(new Order
        {
            BranchId = request.BranchId,
            TotalAmount = total
        }, cancellationToken);

        foreach (var item in request.Items)
        {
            await _inventory.AdjustStockAsync(
                item.ProductId,
                request.BranchId,
                -item.Quantity,
                $"Checkout order {order.Id}");
        }

        await _invoices.CreateAsync(new Invoice
        {
            CustomerId = request.CustomerId,
            BranchId = request.BranchId,
            TotalAmount = total,
            CreatedAt = DateTime.UtcNow
        });

        await _settlements.CreateAsync(new ArtistSettlement
        {
            TotalSalesCost = total,
            IsPaid = false
        }, cancellationToken);

        return new CheckoutResult(true, $"Order {order.Id} created.");
    }
}
