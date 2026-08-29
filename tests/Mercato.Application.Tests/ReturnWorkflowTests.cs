using System;
using System.Collections.Generic;
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

public class ReturnWorkflowTests
{
    [Fact]
    public async Task Return_Uses_Original_Sale_Price_And_Reverses_Stock_Settlement_And_Accounting()
    {
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var returnId = Guid.NewGuid();
        var refundPaymentId = Guid.NewGuid();
        const decimal originalSalePrice = 30m;
        const decimal currentSalePrice = 99m;
        const decimal purchasePrice = 12m;

        var orders = new Mock<IOrderRepository>(MockBehavior.Strict);
        var invoices = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var returns = new Mock<ISalesReturnRepository>(MockBehavior.Strict);
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var inventory = new Mock<IInventoryService>(MockBehavior.Strict);
        var settlements = new Mock<ISettlementService>(MockBehavior.Strict);
        var payments = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var accounting = new Mock<IAccountingTransactionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        orders
            .Setup(repository => repository.GetAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                Id = orderId,
                BranchId = branchId,
                TotalAmount = 120m,
                Items = new List<OrderItem>
                {
                    new() { Id = Guid.NewGuid(), ProductId = productId, Quantity = 4, UnitPrice = originalSalePrice }
                }
            });
        invoices
            .Setup(repository => repository.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Invoice { Id = invoiceId, OrderId = orderId, BranchId = branchId, TotalAmount = 120m });
        returns
            .Setup(repository => repository.GetReturnedQuantityAsync(orderId, productId, It.IsAny<CancellationToken>(), true))
            .ReturnsAsync(1);
        products
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto(productId, "Artist product", "SKU-R", purchasePrice, currentSalePrice, null, artistId));
        unitOfWork
            .Setup(work => work.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<ReturnResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<ReturnResult>> action, CancellationToken cancellationToken) => action(cancellationToken));
        returns
            .Setup(repository => repository.AddAsync(It.IsAny<SalesReturn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesReturn salesReturn, CancellationToken _) =>
            {
                salesReturn.Id = returnId;
                return salesReturn;
            });
        inventory
            .Setup(service => service.AdjustStockAsync(productId, branchId, 2, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        settlements
            .Setup(service => service.RecordReturnAsync(orderId, artistId, productId, 2, purchasePrice, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        payments
            .Setup(repository => repository.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment payment, CancellationToken _) =>
            {
                payment.Id = refundPaymentId;
                return payment;
            });
        accounting
            .Setup(repository => repository.AddAsync(It.IsAny<AccountingTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountingTransaction transaction, CancellationToken _) => transaction);

        var service = new ReturnServiceImplementation(
            orders.Object,
            invoices.Object,
            returns.Object,
            products.Object,
            inventory.Object,
            settlements.Object,
            payments.Object,
            accounting.Object,
            unitOfWork.Object);

        var result = await service.ReturnAsync(new ReturnRequest
        {
            OrderId = orderId,
            RefundMethod = "Card",
            Items = new[]
            {
                new ReturnItem(productId, 1),
                new ReturnItem(productId, 1)
            }
        });

        Assert.Equal(orderId, result.OrderId);
        Assert.Equal(refundPaymentId, result.RefundPaymentId);
        Assert.Equal(60m, result.Total);

        returns.Verify(repository => repository.GetReturnedQuantityAsync(
            orderId, productId, It.IsAny<CancellationToken>(), true), Times.Once);
        returns.Verify(repository => repository.AddAsync(
            It.Is<SalesReturn>(salesReturn =>
                salesReturn.OrderId == orderId &&
                salesReturn.BranchId == branchId &&
                salesReturn.TotalAmount == 60m &&
                salesReturn.RefundMethod == "Card" &&
                salesReturn.Items.Count == 1 &&
                salesReturn.Items.First().ProductId == productId &&
                salesReturn.Items.First().Quantity == 2 &&
                salesReturn.Items.First().UnitPrice == originalSalePrice),
            It.IsAny<CancellationToken>()), Times.Once);
        inventory.Verify(service => service.AdjustStockAsync(
            productId, branchId, 2, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        settlements.Verify(service => service.RecordReturnAsync(
            orderId, artistId, productId, 2, purchasePrice, It.IsAny<CancellationToken>()), Times.Once);
        payments.Verify(repository => repository.AddAsync(
            It.Is<Payment>(payment =>
                payment.OrderId == orderId &&
                payment.Amount == -60m &&
                payment.Method == "Card" &&
                payment.Type == "Refund"),
            It.IsAny<CancellationToken>()), Times.Once);
        accounting.Verify(repository => repository.AddAsync(
            It.Is<AccountingTransaction>(transaction =>
                transaction.OrderId == orderId &&
                transaction.InvoiceId == invoiceId &&
                transaction.BranchId == branchId &&
                transaction.Amount == -60m &&
                transaction.Type == "Refund"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Return_Rejects_Aggregated_Duplicate_Quantity_Above_Remaining_Sold_Quantity()
    {
        var orderId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var orders = new Mock<IOrderRepository>(MockBehavior.Strict);
        var invoices = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var returns = new Mock<ISalesReturnRepository>(MockBehavior.Strict);
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var inventory = new Mock<IInventoryService>(MockBehavior.Strict);
        var settlements = new Mock<ISettlementService>(MockBehavior.Strict);
        var payments = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var accounting = new Mock<IAccountingTransactionRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        unitOfWork
            .Setup(work => work.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<ReturnResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<ReturnResult>> action, CancellationToken cancellationToken) => action(cancellationToken));
        orders
            .Setup(repository => repository.GetAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                Id = orderId,
                BranchId = branchId,
                Items = new List<OrderItem>
                {
                    new() { Id = Guid.NewGuid(), ProductId = productId, Quantity = 3, UnitPrice = 20m }
                }
            });
        invoices
            .Setup(repository => repository.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Invoice { Id = Guid.NewGuid(), OrderId = orderId, BranchId = branchId });
        returns
            .Setup(repository => repository.GetReturnedQuantityAsync(orderId, productId, It.IsAny<CancellationToken>(), true))
            .ReturnsAsync(1);

        var service = new ReturnServiceImplementation(
            orders.Object,
            invoices.Object,
            returns.Object,
            products.Object,
            inventory.Object,
            settlements.Object,
            payments.Object,
            accounting.Object,
            unitOfWork.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReturnAsync(new ReturnRequest
        {
            OrderId = orderId,
            RefundMethod = "Card",
            Items = new[]
            {
                new ReturnItem(productId, 2),
                new ReturnItem(productId, 1)
            }
        }));

        Assert.Equal($"Return quantity exceeds quantity sold for product {productId}.", exception.Message);
        returns.Verify(repository => repository.GetReturnedQuantityAsync(
            orderId, productId, It.IsAny<CancellationToken>(), true), Times.Once);
        returns.Verify(repository => repository.AddAsync(It.IsAny<SalesReturn>(), It.IsAny<CancellationToken>()), Times.Never);
        inventory.VerifyNoOtherCalls();
        payments.VerifyNoOtherCalls();
        accounting.VerifyNoOtherCalls();
    }
}
