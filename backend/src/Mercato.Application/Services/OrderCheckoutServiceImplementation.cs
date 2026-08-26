using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class OrderCheckoutServiceImplementation : IOrderCheckoutService
{
    private readonly IOrderService _orders;
    private readonly IInvoiceService _invoices;
    private readonly IInventoryService _inventory;
    private readonly IProductRepository _products;
    private readonly ISettlementService _settlements;

    public OrderCheckoutServiceImplementation(
        IOrderService orders,
        IInvoiceService invoices,
        IInventoryService inventory,
        IProductRepository products,
        ISettlementService settlements)
    {
        _orders = orders;
        _invoices = invoices;
        _inventory = inventory;
        _products = products;
        _settlements = settlements;
    }

    public async Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        if (request.BranchId == Guid.Empty)
            throw new InvalidOperationException("Checkout requires a branch.");

        if (request.Items.Count == 0)
            throw new InvalidOperationException("Checkout requires items.");

        var orderItems = new List<OrderItem>(request.Items.Count);
        var settlementSales = new List<(Guid ArtistId, Guid ProductId, int Quantity, decimal PurchaseUnitPrice)>();

        foreach (var item in request.Items)
        {
            if (item.ProductId == Guid.Empty || item.Quantity <= 0)
                throw new InvalidOperationException("Checkout contains an invalid item.");

            var product = await _products.GetByIdAsync(item.ProductId, cancellationToken)
                ?? throw new InvalidOperationException($"Product {item.ProductId} was not found.");

            var available = await _inventory.GetAvailableQuantityAsync(item.ProductId, request.BranchId);
            if (available < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}.");

            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                UnitPrice = product.SalePrice,
                Quantity = item.Quantity
            });

            if (product.ArtistId is Guid artistId && artistId != Guid.Empty)
            {
                settlementSales.Add((
                    artistId,
                    item.ProductId,
                    item.Quantity,
                    product.PurchasePrice));
            }
        }

        var total = orderItems.Sum(x => x.Quantity * x.UnitPrice);

        var order = await _orders.CreateAsync(new Order
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            TotalAmount = total,
            Items = orderItems
        }, cancellationToken);

        foreach (var item in orderItems)
        {
            await _inventory.AdjustStockAsync(
                item.ProductId,
                request.BranchId,
                -item.Quantity,
                $"Checkout order {order.Id}");
        }

        var invoice = await _invoices.CreateAsync(new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            CustomerId = request.CustomerId,
            BranchId = request.BranchId,
            TotalAmount = total,
            CreatedAt = DateTime.UtcNow
        });

        foreach (var sale in settlementSales)
        {
            await _settlements.RecordSaleAsync(
                order.Id,
                sale.ArtistId,
                sale.ProductId,
                sale.Quantity,
                sale.PurchaseUnitPrice,
                cancellationToken);
        }

        return new CheckoutResult(true, $"Order {order.Id} and invoice {invoice.Id} created.");
    }
}
