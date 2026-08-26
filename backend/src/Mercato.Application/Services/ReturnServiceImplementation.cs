using Mercato.Application.DTOs;
using Mercato.Application.Interfaces;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class ReturnServiceImplementation : IReturnService
{
    private readonly IOrderRepository _orders;
    private readonly IInvoiceRepository _invoices;
    private readonly ISalesReturnRepository _returns;
    private readonly IProductRepository _products;
    private readonly IInventoryService _inventory;
    private readonly ISettlementService _settlements;
    private readonly IPaymentRepository _payments;
    private readonly IAccountingTransactionRepository _accounting;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnServiceImplementation(
        IOrderRepository orders,
        IInvoiceRepository invoices,
        ISalesReturnRepository returns,
        IProductRepository products,
        IInventoryService inventory,
        ISettlementService settlements,
        IPaymentRepository payments,
        IAccountingTransactionRepository accounting,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _invoices = invoices;
        _returns = returns;
        _products = products;
        _inventory = inventory;
        _settlements = settlements;
        _payments = payments;
        _accounting = accounting;
        _unitOfWork = unitOfWork;
    }

    public Task<ReturnResult> ReturnAsync(ReturnRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OrderId == Guid.Empty)
            throw new InvalidOperationException("Return requires an order.");
        if (string.IsNullOrWhiteSpace(request.RefundMethod))
            throw new InvalidOperationException("Return requires a refund method.");
        if (request.Items.Count == 0)
            throw new InvalidOperationException("Return requires items.");

        return _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _orders.GetAsync(request.OrderId, ct)
                ?? throw new InvalidOperationException("Order was not found.");
            var invoice = await _invoices.GetByOrderIdAsync(order.Id, ct)
                ?? throw new InvalidOperationException("Order invoice was not found.");

            var returnLines = new List<SalesReturnLine>();
            var settlementReversals = new List<(Guid ArtistId, Guid ProductId, int Quantity, decimal PurchasePrice)>();

            foreach (var requested in request.Items)
            {
                if (requested.ProductId == Guid.Empty || requested.Quantity <= 0)
                    throw new InvalidOperationException("Return contains an invalid item.");

                var sold = order.Items.Where(x => x.ProductId == requested.ProductId).Sum(x => x.Quantity);
                if (sold == 0)
                    throw new InvalidOperationException($"Product {requested.ProductId} was not sold on this order.");

                var alreadyReturned = await _returns.GetReturnedQuantityAsync(order.Id, requested.ProductId, ct);
                if (alreadyReturned + requested.Quantity > sold)
                    throw new InvalidOperationException($"Return quantity exceeds quantity sold for product {requested.ProductId}.");

                var orderItem = order.Items.First(x => x.ProductId == requested.ProductId);
                returnLines.Add(new SalesReturnLine
                {
                    Id = Guid.NewGuid(),
                    ProductId = requested.ProductId,
                    Quantity = requested.Quantity,
                    UnitPrice = orderItem.UnitPrice
                });

                var product = await _products.GetByIdAsync(requested.ProductId, ct)
                    ?? throw new InvalidOperationException($"Product {requested.ProductId} was not found.");
                if (product.ArtistId is Guid artistId && artistId != Guid.Empty)
                    settlementReversals.Add((artistId, requested.ProductId, requested.Quantity, product.PurchasePrice));
            }

            var total = returnLines.Sum(x => x.Quantity * x.UnitPrice);
            var createdAtUtc = DateTime.UtcNow;
            var reference = $"RET-{createdAtUtc:yyyyMMdd}-{Guid.NewGuid():N}"[..25];

            var salesReturn = new SalesReturn
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                BranchId = order.BranchId,
                TotalAmount = total,
                RefundMethod = request.RefundMethod.Trim(),
                Reference = reference,
                CreatedAtUtc = createdAtUtc,
                Items = returnLines
            };
            foreach (var line in returnLines)
                line.SalesReturnId = salesReturn.Id;

            await _returns.AddAsync(salesReturn, ct);

            foreach (var line in returnLines)
                await _inventory.AdjustStockAsync(line.ProductId, order.BranchId, line.Quantity, $"Return {salesReturn.Id}");

            foreach (var reversal in settlementReversals)
                await _settlements.RecordReturnAsync(order.Id, reversal.ArtistId, reversal.ProductId, reversal.Quantity, reversal.PurchasePrice, ct);

            var refund = await _payments.AddAsync(new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = -total,
                Method = salesReturn.RefundMethod,
                Type = "Refund",
                Reference = reference,
                PaidAt = createdAtUtc
            }, ct);

            await _accounting.AddAsync(new AccountingTransaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                InvoiceId = invoice.Id,
                BranchId = order.BranchId,
                Amount = -total,
                Type = "Refund",
                Description = $"Return {salesReturn.Id} refunded by {salesReturn.RefundMethod}",
                CreatedAtUtc = createdAtUtc
            }, ct);

            return new ReturnResult(salesReturn.Id, order.Id, refund.Id, total, reference, createdAtUtc);
        }, cancellationToken);
    }
}
