using Mercato.Application.DTOs;
using Mercato.Application.Interfaces;
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
    private readonly IPaymentRepository _payments;
    private readonly IAccountingTransactionRepository _accountingTransactions;
    private readonly IUnitOfWork _unitOfWork;

    public OrderCheckoutServiceImplementation(
        IOrderService orders,
        IInvoiceService invoices,
        IInventoryService inventory,
        IProductRepository products,
        ISettlementService settlements,
        IPaymentRepository payments,
        IAccountingTransactionRepository accountingTransactions,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _invoices = invoices;
        _inventory = inventory;
        _products = products;
        _settlements = settlements;
        _payments = payments;
        _accountingTransactions = accountingTransactions;
        _unitOfWork = unitOfWork;
    }

    public Task<CheckoutResult> CheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.BranchId == Guid.Empty)
            throw new InvalidOperationException("Checkout requires a branch.");

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            throw new InvalidOperationException("Checkout requires a payment method.");

        if (request.Items.Count == 0)
            throw new InvalidOperationException("Checkout requires items.");

        return _unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                var orderItems = new List<OrderItem>(request.Items.Count);
                var settlementSales = new List<(Guid ArtistId, Guid ProductId, int Quantity, decimal PurchaseUnitPrice)>();

                foreach (var item in request.Items)
                {
                    if (item.ProductId == Guid.Empty || item.Quantity <= 0)
                        throw new InvalidOperationException("Checkout contains an invalid item.");

                    var product = await _products.GetByIdAsync(item.ProductId, transactionCancellationToken)
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
                }, transactionCancellationToken);

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
                        transactionCancellationToken);
                }

                var payment = await _payments.AddAsync(new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Amount = total,
                    Method = request.PaymentMethod.Trim(),
                    Reference = $"POS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25],
                    PaidAt = DateTime.UtcNow
                }, transactionCancellationToken);

                await _accountingTransactions.AddAsync(new AccountingTransaction
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    InvoiceId = invoice.Id,
                    BranchId = request.BranchId,
                    Amount = total,
                    Type = "Sale",
                    Description = $"POS sale paid by {payment.Method}",
                    CreatedAtUtc = DateTime.UtcNow
                }, transactionCancellationToken);

                return new CheckoutResult
                {
                    OrderId = order.Id,
                    InvoiceId = invoice.Id,
                    PaymentId = payment.Id,
                    Total = total,
                    ReceiptReference = payment.Reference,
                    Status = "Completed"
                };
            },
            cancellationToken);
    }
}
