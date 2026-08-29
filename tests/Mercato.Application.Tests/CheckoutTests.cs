using System;
using System.Threading;
using System.Threading.Tasks;
using Mercato.Application.DTOs;
using Mercato.Application.Interfaces;
using Mercato.Application.Repositories;
using Mercato.Application.Services;
using Moq;
using Xunit;

namespace Mercato.Application.Tests;

public class CheckoutTests
{
    [Fact]
    public async Task Checkout_Rejects_Missing_Idempotency_Key()
    {
        var fixture = CreateFixture();

        var request = new CheckoutRequest
        {
            BranchId = Guid.NewGuid(),
            PaymentMethod = "Cash",
            Items = new[] { new CheckoutItem { ProductId = Guid.NewGuid(), Quantity = 1 } }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CheckoutAsync(request));

        Assert.Equal("Checkout requires an idempotency key.", exception.Message);
        fixture.UnitOfWork.Verify(
            unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<CheckoutResult>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Checkout_Rejects_Unknown_Customer_Before_Transaction()
    {
        var fixture = CreateFixture();
        var customerId = Guid.NewGuid();
        fixture.Customers
            .Setup(repository => repository.ExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new CheckoutRequest
        {
            BranchId = Guid.NewGuid(),
            CustomerId = customerId,
            PaymentMethod = "Cash",
            IdempotencyKey = "customer-validation",
            Items = new[] { new CheckoutItem { ProductId = Guid.NewGuid(), Quantity = 1 } }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CheckoutAsync(request));

        Assert.Equal("Checkout customer was not found.", exception.Message);
        fixture.UnitOfWork.Verify(
            unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<CheckoutResult>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Checkout_Aggregates_Duplicate_Product_Lines_Before_Stock_Validation()
    {
        var fixture = CreateFixture();
        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        fixture.Idempotency
            .Setup(repository => repository.GetAsync("duplicate-lines", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Mercato.Domain.Entities.CheckoutIdempotencyRecord?)null);
        fixture.Products
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto(productId, "Test product", "SKU-1", 4m, 10m, null, null));
        fixture.Inventory
            .Setup(service => service.GetAvailableQuantityAsync(productId, branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        fixture.UnitOfWork
            .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<CheckoutResult>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<CheckoutResult>> action, CancellationToken cancellationToken) => action(cancellationToken));

        var request = new CheckoutRequest
        {
            BranchId = branchId,
            PaymentMethod = "Cash",
            IdempotencyKey = "duplicate-lines",
            Items = new[]
            {
                new CheckoutItem { ProductId = productId, Quantity = 3 },
                new CheckoutItem { ProductId = productId, Quantity = 3 }
            }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CheckoutAsync(request));

        Assert.Equal($"Insufficient stock for product {productId}.", exception.Message);
        fixture.Inventory.Verify(
            service => service.GetAvailableQuantityAsync(productId, branchId, It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.Orders.Verify(
            service => service.CreateAsync(It.IsAny<Mercato.Domain.Entities.Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static CheckoutFixture CreateFixture()
    {
        var orders = new Mock<IOrderService>(MockBehavior.Strict);
        var invoices = new Mock<IInvoiceService>(MockBehavior.Strict);
        var inventory = new Mock<IInventoryService>(MockBehavior.Strict);
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var customers = new Mock<ICustomerRepository>(MockBehavior.Strict);
        var settlements = new Mock<ISettlementService>(MockBehavior.Strict);
        var payments = new Mock<IPaymentRepository>(MockBehavior.Strict);
        var accountingTransactions = new Mock<IAccountingTransactionRepository>(MockBehavior.Strict);
        var idempotency = new Mock<ICheckoutIdempotencyRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);

        var service = new OrderCheckoutServiceImplementation(
            orders.Object,
            invoices.Object,
            inventory.Object,
            products.Object,
            customers.Object,
            settlements.Object,
            payments.Object,
            accountingTransactions.Object,
            idempotency.Object,
            unitOfWork.Object);

        return new CheckoutFixture(service, orders, inventory, products, customers, idempotency, unitOfWork);
    }

    private sealed record CheckoutFixture(
        OrderCheckoutServiceImplementation Service,
        Mock<IOrderService> Orders,
        Mock<IInventoryService> Inventory,
        Mock<IProductRepository> Products,
        Mock<ICustomerRepository> Customers,
        Mock<ICheckoutIdempotencyRepository> Idempotency,
        Mock<IUnitOfWork> UnitOfWork);
}
