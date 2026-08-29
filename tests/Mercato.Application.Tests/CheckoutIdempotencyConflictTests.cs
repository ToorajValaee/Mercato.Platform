using System;
using System.Text.Json;
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

public class CheckoutIdempotencyConflictTests
{
    [Fact]
    public async Task Checkout_Reloads_Completed_Result_When_First_Write_Loses_Idempotency_Race()
    {
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

        var branchId = Guid.NewGuid();
        const string key = "nop:race-42";
        var expected = new CheckoutResult
        {
            OrderId = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            BranchId = branchId,
            Total = 48m,
            PaymentMethod = "Online",
            ReceiptReference = "NOP-RACE",
            PaidAtUtc = DateTime.UtcNow,
            Status = "Completed"
        };

        idempotency
            .SetupSequence(repository => repository.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutIdempotencyRecord?)null)
            .ReturnsAsync(new CheckoutIdempotencyRecord
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = key,
                ResponseJson = JsonSerializer.Serialize(expected),
                CreatedAtUtc = DateTime.UtcNow
            });
        branches
            .Setup(repository => repository.GetAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Branch { Id = branchId, Name = "Main" });
        unitOfWork
            .Setup(work => work.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<CheckoutResult>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CheckoutIdempotencyConflictException(key, new InvalidOperationException("unique violation")));

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
            PaymentMethod = "Online",
            IdempotencyKey = key,
            Items = new[] { new CheckoutItem { ProductId = Guid.NewGuid(), Quantity = 1 } }
        });

        Assert.Equal(expected.OrderId, result.OrderId);
        Assert.Equal(expected.InvoiceId, result.InvoiceId);
        Assert.Equal(expected.PaymentId, result.PaymentId);
        Assert.Equal(expected.BranchId, result.BranchId);
        Assert.Equal(expected.Total, result.Total);
        Assert.Equal(expected.ReceiptReference, result.ReceiptReference);

        idempotency.Verify(repository => repository.GetAsync(key, It.IsAny<CancellationToken>()), Times.Exactly(2));
        branches.Verify(repository => repository.GetAsync(branchId, It.IsAny<CancellationToken>()), Times.Once);
        orders.VerifyNoOtherCalls();
        invoices.VerifyNoOtherCalls();
        inventory.VerifyNoOtherCalls();
        products.VerifyNoOtherCalls();
        customers.VerifyNoOtherCalls();
        settlements.VerifyNoOtherCalls();
        payments.VerifyNoOtherCalls();
        accounting.VerifyNoOtherCalls();
    }
}
