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
    private readonly IBranchRepository _branches;
    private readonly ICustomerRepository _customers;
    private readonly ISettlementService _settlements;
    private readonly IPaymentRepository _payments;
    private readonly IPaymentMethodRepository _paymentMethods;
    private readonly IDiscountRepository _discounts;
    private readonly IAccountingTransactionRepository _accountingTransactions;
    private readonly ICheckoutIdempotencyRepository _idempotency;
    private readonly IUnitOfWork _unitOfWork;

    public OrderCheckoutServiceImplementation(
        IOrderService orders,
        IInvoiceService invoices,
        IInventoryService inventory,
        IProductRepository products,
        IBranchRepository branches,
        ICustomerRepository customers,
        ISettlementService settlements,
        IPaymentRepository payments,
        IPaymentMethodRepository paymentMethods,
        IDiscountRepository discounts,
        IAccountingTransactionRepository accountingTransactions,
        ICheckoutIdempotencyRepository idempotency,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _invoices = invoices;
        _inventory = inventory;
        _products = products;
        _branches = branches;
        _customers = customers;
        _settlements = settlements;
        _payments = payments;
        _paymentMethods = paymentMethods;
        _discounts = discounts;
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

        if (request.PaymentMethodId is null && string.IsNullOrWhiteSpace(request.PaymentMethod))
            throw new InvalidOperationException("Checkout requires a payment method.");

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new InvalidOperationException("Checkout requires an idempotency key.");

        var idempotencyKey = request.IdempotencyKey.Trim();
        if (idempotencyKey.Length > 100)
            throw new InvalidOperationException("Checkout idempotency key cannot exceed 100 characters.");

        if (request.Items.Count == 0)
            throw new InvalidOperationException("Checkout requires items.");

        if (request.Items.Any(item => item.ProductId == Guid.Empty || item.Quantity <= 0))
            throw new InvalidOperationException("Checkout contains an invalid item.");

        IReadOnlyList<CheckoutItem> normalizedItems;
        try
        {
            normalizedItems = request.Items
                .GroupBy(item => item.ProductId)
                .Select(group => new CheckoutItem
                {
                    ProductId = group.Key,
                    Quantity = group.Sum(item => item.Quantity)
                })
                .OrderBy(item => item.ProductId)
                .ToList();
        }
        catch (OverflowException exception)
        {
            throw new InvalidOperationException("Checkout product quantity is too large.", exception);
        }

        var existing = await _idempotency.GetAsync(idempotencyKey, cancellationToken);
        if (existing is not null)
            return DeserializeResult(existing);

        if (await _branches.GetAsync(request.BranchId, cancellationToken) is null)
            throw new InvalidOperationException("Checkout branch was not found.");

        if (request.CustomerId != Guid.Empty && !await _customers.ExistsAsync(request.CustomerId, cancellationToken))
            throw new InvalidOperationException("Checkout customer was not found.");

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(
                async transactionCancellationToken =>
                {
                    var existingInsideTransaction = await _idempotency.GetAsync(idempotencyKey, transactionCancellationToken);
                    if (existingInsideTransaction is not null)
                        return DeserializeResult(existingInsideTransaction);

                    if (await _branches.GetAsync(request.BranchId, transactionCancellationToken) is null)
                        throw new InvalidOperationException("Checkout branch was not found.");

                    var paymentMethod = await ResolvePaymentMethodAsync(request, transactionCancellationToken);
                    var discount = await ResolveDiscountAsync(request.DiscountId, transactionCancellationToken);

                    var orderItems = new List<OrderItem>(normalizedItems.Count);
                    var receiptLines = new List<ReceiptLine>(normalizedItems.Count);
                    var settlementSales = new List<(Guid ArtistId, Guid ProductId, int Quantity, decimal PurchaseUnitPrice)>();

                    foreach (var item in normalizedItems)
                    {
                        var product = await _products.GetByIdAsync(item.ProductId, transactionCancellationToken)
                            ?? throw new InvalidOperationException($"Product {item.ProductId} was not found.");

                        var available = await _inventory.GetAvailableQuantityAsync(item.ProductId, request.BranchId, transactionCancellationToken);
                        if (available < item.Quantity)
                            throw new InvalidOperationException($"Insufficient stock for product {product.Name}.");

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

                    var subtotal = receiptLines.Sum(x => x.LineTotal);
                    var discountAmount = CalculateDiscount(subtotal, discount);
                    var total = subtotal - discountAmount;

                    var order = await _orders.CreateAsync(new Order
                    {
                        Id = Guid.NewGuid(),
                        BranchId = request.BranchId,
                        SubtotalAmount = subtotal,
                        DiscountId = discount?.Id,
                        DiscountName = discount?.Name,
                        DiscountAmount = discountAmount,
                        TotalAmount = total,
                        Items = orderItems
                    }, transactionCancellationToken);

                    foreach (var item in orderItems)
                        await _inventory.AdjustStockAsync(item.ProductId, request.BranchId, -item.Quantity, $"Checkout order {order.Id}", transactionCancellationToken);

                    var invoice = await _invoices.CreateAsync(new Invoice
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        CustomerId = request.CustomerId,
                        BranchId = request.BranchId,
                        SubtotalAmount = subtotal,
                        DiscountName = discount?.Name,
                        DiscountAmount = discountAmount,
                        TotalAmount = total,
                        CreatedAt = DateTime.UtcNow,
                        Items = orderItems.Select(item => new InvoiceItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        }).ToList()
                    }, transactionCancellationToken);

                    foreach (var sale in settlementSales)
                        await _settlements.RecordSaleAsync(order.Id, sale.ArtistId, sale.ProductId, sale.Quantity, sale.PurchaseUnitPrice, transactionCancellationToken);

                    var paidAtUtc = DateTime.UtcNow;
                    var payment = await _payments.AddAsync(new Payment
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        Amount = total,
                        Method = paymentMethod.Name,
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
                        Description = discountAmount > 0
                            ? $"POS sale paid by {payment.Method}; discount {discount?.Name}: {discountAmount:0.00}"
                            : $"POS sale paid by {payment.Method}",
                        CreatedAtUtc = paidAtUtc
                    }, transactionCancellationToken);

                    var result = new CheckoutResult
                    {
                        OrderId = order.Id,
                        InvoiceId = invoice.Id,
                        PaymentId = payment.Id,
                        BranchId = request.BranchId,
                        Subtotal = subtotal,
                        DiscountId = discount?.Id,
                        DiscountName = discount?.Name,
                        DiscountAmount = discountAmount,
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

    private async Task<PaymentMethod> ResolvePaymentMethodAsync(CheckoutRequest request, CancellationToken cancellationToken)
    {
        PaymentMethod? method = null;
        if (request.PaymentMethodId is Guid methodId && methodId != Guid.Empty)
            method = await _paymentMethods.GetAsync(methodId, cancellationToken);
        else if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
            method = (await _paymentMethods.GetAllAsync(true, cancellationToken))
                .FirstOrDefault(x => x.Name.Equals(request.PaymentMethod.Trim(), StringComparison.OrdinalIgnoreCase));

        if (method is null || !method.IsActive)
            throw new InvalidOperationException("Selected payment method is not available.");
        return method;
    }

    private async Task<DiscountDefinition?> ResolveDiscountAsync(Guid? discountId, CancellationToken cancellationToken)
    {
        if (discountId is null || discountId == Guid.Empty) return null;
        var discount = await _discounts.GetAsync(discountId.Value, cancellationToken);
        if (discount is null || !discount.IsActive)
            throw new InvalidOperationException("Selected discount is not available.");
        return discount;
    }

    private static decimal CalculateDiscount(decimal subtotal, DiscountDefinition? discount)
    {
        if (discount is null || subtotal <= 0) return 0m;
        var amount = discount.Type.Equals("Percent", StringComparison.OrdinalIgnoreCase)
            ? subtotal * discount.Value / 100m
            : discount.Value;
        amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        return Math.Clamp(amount, 0m, subtotal);
    }

    private static CheckoutResult DeserializeResult(CheckoutIdempotencyRecord record)
        => JsonSerializer.Deserialize<CheckoutResult>(record.ResponseJson)
            ?? throw new InvalidOperationException("Stored checkout idempotency result is invalid.");
}
