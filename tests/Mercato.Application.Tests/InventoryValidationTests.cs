using System;
using System.Threading;
using System.Threading.Tasks;
using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
using Mercato.Application.Services;
using Mercato.Domain.Entities;
using Moq;
using Xunit;

namespace Mercato.Application.Tests;

public class InventoryValidationTests
{
    [Fact]
    public async Task AdjustStock_Rejects_Unknown_Product_Before_Ledger_Write()
    {
        var inventory = new Mock<IInventoryRepository>(MockBehavior.Strict);
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var branches = new Mock<IBranchRepository>(MockBehavior.Strict);
        var productId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        products
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        var service = new InventoryServiceImplementation(inventory.Object, products.Object, branches.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AdjustStockAsync(productId, branchId, 3m, "Receive"));

        Assert.Equal("Product was not found.", exception.Message);
        inventory.VerifyNoOtherCalls();
        branches.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AdjustStock_Rejects_Unknown_Branch_Before_Ledger_Write()
    {
        var inventory = new Mock<IInventoryRepository>(MockBehavior.Strict);
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var branches = new Mock<IBranchRepository>(MockBehavior.Strict);
        var productId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        products
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto(productId, "Product", "SKU", 4m, 10m, null, null));
        branches
            .Setup(repository => repository.GetAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

        var service = new InventoryServiceImplementation(inventory.Object, products.Object, branches.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AdjustStockAsync(productId, branchId, 3m, "Receive"));

        Assert.Equal("Branch was not found.", exception.Message);
        inventory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AdjustStock_Rejects_Fractional_Quantity_Before_Ledger_Write()
    {
        var inventory = new Mock<IInventoryRepository>(MockBehavior.Strict);
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var branches = new Mock<IBranchRepository>(MockBehavior.Strict);
        var productId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        products
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductDto(productId, "Product", "SKU", 4m, 10m, null, null));
        branches
            .Setup(repository => repository.GetAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Branch { Id = branchId, Name = "Main" });

        var service = new InventoryServiceImplementation(inventory.Object, products.Object, branches.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AdjustStockAsync(productId, branchId, 1.5m, "Receive"));

        Assert.Equal("quantity", exception.ParamName);
        Assert.Contains("whole number", exception.Message, StringComparison.OrdinalIgnoreCase);
        inventory.VerifyNoOtherCalls();
    }
}
