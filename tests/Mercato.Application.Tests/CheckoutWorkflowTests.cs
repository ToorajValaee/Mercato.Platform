using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mercato.Application.DTOs;
using Mercato.Application.Interfaces;
using Mercato.Application.Repositories;
using Mercato.Application.Services;
using Mercato.Domain.Entities;
using Moq;
using Xunit;

namespace Mercato.Application.Tests;

public class CheckoutWorkflowTests
{
    [Fact]
    public async Task Checkout_Commits_One_Coherent_Authoritative_Sale_Workflow()
    {
        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        const decimal purchasePrice = 11m;
        const decimal salePrice = 25m;

        var orders = new Mock<IOrderService>(MockBehavior.Strict);
        var invoices = new Mock<IInvoiceService>(MockBehavior.Strict);
        var inventory = new Mock<IInventoryService>(MockBehavior.Strict);
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var branches = new Mock<IBranchRepository>(MockBehavior.Strict);
        var customers = new Mock<ICustomerRepository>(MockBehavior.Strict);
        var settlements = new Mock<ISettlementService>(MockBehavior.Strict);
        var payments = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var accounting = new Mock<IAccountingTransactionRepository>(MockBehavior.Strict);
        var idempotency = new Mock<ICheckoutIdempotencyRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        idempotency
            .Setup(repository => repository.GetAsync("pos-success", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutIdempotencyRecord?)null);
        branches
            .Setup(repository => repository.GetAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Branch { Id = branchId, Name = "Main" });
        customers
            .Setup(repository => repository.ExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        unitOfWork
            .Setup(work => work.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<CheckoutResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<CheckoutResult>> action, CancellationToken cancellationToken) => action(cancellationToken));
        products
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto(productId, "Artist product", "SKU-ART", purchasePrice, salePrice, null, artistId));
        inventory
            .Setup(service => service.GetAvailableQuantityAsync(productId, branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        orders
            .Setup(service => service.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken _) =>
            {
                order.Id = orderId;
                return order;
            });
        inventory
            .Setup(service => service.AdjustStockAsync(productId, branchId, -3, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        invoices
            .Setup(service => service.CreateAsync(It.IsAny<Invoice>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invoice invoice, CancellationToken _) =>
            {
                invoice.Id = invoiceId;
                return invoice;
            });
        settlements
            .Setup(service => service.RecordSaleAsync(orderId, artistId, productId, 3, purchasePrice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        payments
            .Setup(repository => repository.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment payment, CancellationToken _) =>
            {
                payment.Id = paymentId;
                return payment;
            });
        accounting
            .Setup(repository => repository.AddAsync(It.IsAny<AccountingTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountingTransaction transaction, CancellationToken _) => transaction);
        idempotency
            .Setup(repository => repository.AddAsync(It.IsAny<CheckoutIdempotencyRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new OrderCheckoutServiceImplementation(
            orders.Object,
            invoices.Object,
            inventory.Object,
            products.Object,
            branches.Object,
            customers.Object,
            settlements.Object,
            payments.Object,
            accounting.Object,
            idempotency.Object,
            unitOfWork.Object);

        var result = await service.CheckoutAsync(new CheckoutRequest
        {
            BranchId = branchId,
            CustomerId = customerId,
            PaymentMethod = "Card",
            IdempotencyKey = "pos-success",
            Items = new[]
            {
                new CheckoutItem { ProductId = productId, Quantity = 1 },
                new CheckoutItem { ProductId = productId, Quantity = 2 }
            }
        });

        Assert.Equal(orderId, result.OrderId);
        Assert.Equal(invoiceId, result.InvoiceId);
        Assert.Equal(paymentId, result.PaymentId);
        Assert.Equal(branchId, result.BranchId);
        Assert.Equal(75m, result.Total);
        Assert.Equal("Card", result.PaymentMethod);
        Assert.Equal("Completed", result.Status);
        var receiptLine = Assert.Single(result.Items);
        Assert.Equal(productId, receiptLine.ProductId);
        Assert.Equal(3, receiptLine.Quantity);
        Assert.Equal(salePrice, receiptLine.UnitPrice);
        Assert.Equal(75m, receiptLine.LineTotal);

        orders.Verify(service => service.CreateAsync(
            It.Is<Order>(order =>
                order.BranchId == branchId &&
                order.TotalAmount == 75m &&
                order.Items.Count == 1 &&
                order.Items.Single().ProductId == productId &&
                order.Items.Single().Quantity == 3 &&
                order.Items.Single().UnitPrice == salePrice),
            It.IsAny<CancellationToken>()), Times.Once);
        inventory.Verify(service => service.AdjustStockAsync(
            productId, branchId, -3, It.Is<string>(reason => reason.Contains(orderId.ToString())), It.IsAny<CancellationToken>()), Times.Once);
        invoices.Verify(service => service.CreateAsync(
            It.Is<Invoice>(invoice =>
                invoice.OrderId == orderId &&
                invoice.CustomerId == customerId &&
                invoice.BranchId == branchId &&
                invoice.TotalAmount == 75m),
            It.IsAny<CancellationToken>()), Times.Once);
        settlements.Verify(service => service.RecordSaleAsync(
            orderId, artistId, productId, 3, purchasePrice, It.IsAny<CancellationToken>()), Times.Once);
        payments.Verify(repository => repository.AddAsync(
            It.Is<Payment>(payment =>
                payment.OrderId == orderId &&
                payment.Amount == 75m &&
                payment.Method == "Card" &&
                payment.Type == "Payment"),
            It.IsAny<CancellationToken>()), Times.Once);
        accounting.Verify(repository => repository.AddAsync(
            It.Is<AccountingTransaction>(transaction =>
                transaction.OrderId == orderId &&
                transaction.InvoiceId == invoiceId &&
                transaction.BranchId == branchId &&
                transaction.Amount == 75m &&
                transaction.Type == "Sale"),
            It.IsAny<CancellationToken>()), Times.Once);
        idempotency.Verify(repository => repository.AddAsync(
            It.Is<CheckoutIdempotencyRecord>(record =>
                record.IdempotencyKey == "pos-success" &&
                record.ResponseJson.Contains(orderId.ToString())),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
