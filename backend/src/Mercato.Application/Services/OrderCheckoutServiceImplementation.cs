using System.Text.Json;
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
    private readonly ICustomerRepository _customers;
    private readonly ISettlementService _settlements;
    private readonly IPaymentRepository _payments;
    private readonly IAccountingTransactionRepository _accountingTransactions;
    private readonly ICheckoutIdempotencyRepository _idempotency;
    private readonly IUnitOfWork _unitOfWork;

    public OrderCheckoutServiceImplementation(
        IOrderService orders,
        IInvoiceService invoices,
        IInventoryService inventory,
        IProductRepository products,
        ICustomerRepository customers,
        ISettlementService settlements,
        IPaymentRepository payments,
        IAccountingTransactionRepository accountingTransactions,
        ICheckoutIdempotencyRepository idempotency,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _invoices = invoices;
        _inventory = inventory;
        _products = products;
        _customers = customers;
        _settlements = settlements;
        _payments = payments;
        _accountingTransactions = accountingTransactions;
        _idempotency = idempotency;
        _unitOfWork = unitOfWork;
    }

    public async Task<CheckoutResult> CheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.BranchId == Guid.Empty)
            throw new InvalidOperationException("Checkout requires a branch.");

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            throw new InvalidOperationException("Checkout requires a payment method.");

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new InvalidOperationException("Checkout requires an idempotency key.");

        var idempotencyKey = request.IdempotencyKey.Trim();
        if (idempotencyKey.Length > 100)
            throw new InvalidOperationException("Checkout idempotency key cannot exceed 100 characters.");

        if (request.Items.Count == 0)
            throw new InvalidOperationException("Checkout requires items.");

        if (request.CustomerId != Guid.Empty && !await _customers.ExistsAsync(request.CustomerId, cancellationToken))
            throw new InvalidOperationException("Checkout customer was not found.");

        var existing = await _idempotency.GetAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
            return DeserializeResult(existing);

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                async transactionCancellationToken =>
                {
                    var existingInsideTransaction = await _idempotency.GetAsync(idempotencyKey, transactionCancellationToken);
                    if (existingInsideTransaction is not null)
                        return DeserializeResult(existingInsideTransaction);

                    var orderItems = new List<OrderItem>(request.Items.Count);
                    var receiptLines = new List<ReceiptLine>(request.Items.Count);
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

                        var lineTotal = item.Quantity * product.SalePrice;
                        orderItems.Add(new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = item.ProductId,
                            UnitPrice = product.SalePrice,
                            Quantity = item.Quantity
                        });

                        receiptLines.Add(new ReceiptLine(item.ProductId, product.Name, item.Quantity, product.SalePrice, lineTotal));

                        if (product.ArtistId is Guid artistId && artistId != Guid.Empty)
                            settlementSales.Add((artistId, item.ProductId, item.Quantity, product.PurchasePrice));
                    }

                    var total = receiptLines.Sum(x => x.LineTotal);
                    var order = await _orders.CreateAsync(new Order
                    {
                        Id = Guid.NewGuid(),
                        BranchId = request.BranchId,
                        TotalAmount = total,
                        Items = orderItems
                    }, transactionCancellationToken);

                    foreach (var item in orderItems)
                        await _inventory.AdjustStockAsync(item.ProductId, request.BranchId, -item.Quantity, $"Checkout order {order.Id}");

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
                        await _settlements.RecordSaleAsync(order.Id, sale.ArtistId, sale.ProductId, sale.Quantity, sale.PurchaseUnitPrice, transactionCancellationToken);

                    var paidAtUtc = DateTime.UtcNow;
                    var paymentMethod = request.PaymentMethod.Trim();
                    var payment = await _payments.AddAsync(new Payment
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        Amount = total,
                        Method = paymentMethod,
                        Type = "Payment",
                        Reference = $"POS-{paidAtUtc:yyyyMMdd}-{Guid.NewGuid():N}"[..25],
                        PaidAt = paidAtUtc
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
                        CreatedAtUtc = paidAtUtc
                    }, transactionCancellationToken);

                    var result = new CheckoutResult
                    {
                        OrderId = order.Id,
                        InvoiceId = invoice.Id,
                        PaymentId = payment.Id,
                        BranchId = request.BranchId,
                        Total = total,
                        PaymentMethod = payment.Method,
                        ReceiptReference = payment.Reference,
                        PaidAtUtc = paidAtUtc,
                        Items = receiptLines,
                        Status = "Completed"
                    };

                    await _idempotency.AddAsync(new CheckoutIdempotencyRecord
                    {
                        Id = Guid.NewGuid(),
                        IdempotencyKey = idempotencyKey,
                        ResponseJson = JsonSerializer.Serialize(result),
                        CreatedAtUtc = DateTime.UtcNow
                    }, transactionCancellationToken);

                    return result;
                }, cancellationToken);
        }
        catch (CheckoutIdempotencyConflictException)
        {
            var completed = await _idempotency.GetAsync(idempotencyKey, cancellationToken);
            if (completed is not null)
                return DeserializeResult(completed);
            throw;
        }
    }

    private static CheckoutResult DeserializeResult(CheckoutIdempotencyRecord record)
        => JsonSerializer.Deserialize<CheckoutResult>(record.ResponseJson)
            ?? throw new InvalidOperationException("Stored checkout idempotency result is invalid.");
}
