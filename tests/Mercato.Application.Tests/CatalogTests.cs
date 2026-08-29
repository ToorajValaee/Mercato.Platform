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

public class CatalogTests
{
    [Fact]
    public async Task Catalog_Rejects_Unknown_Branch_Before_Loading_Products()
    {
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var branches = new Mock<IBranchRepository>(MockBehavior.Strict);
        var inventory = new Mock<IInventoryService>(MockBehavior.Strict);
        var branchId = Guid.NewGuid();

        branches
            .Setup(repository => repository.GetAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Branch?)null);

        var service = new ProductCatalogServiceImplementation(products.Object, branches.Object, inventory.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetCatalogAsync(branchId));

        Assert.Equal("Catalog branch was not found.", exception.Message);
        products.VerifyNoOtherCalls();
        inventory.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Catalog_Returns_Authoritative_Price_And_Selected_Branch_Availability()
    {
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var branches = new Mock<IBranchRepository>(MockBehavior.Strict);
        var inventory = new Mock<IInventoryService>(MockBehavior.Strict);
        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        branches
            .Setup(repository => repository.GetAsync(branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Branch { Id = branchId, Name = "Online" });
        products
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProductDto(productId, "Store product", "SKU-ONLINE", 12m, 29m, null, null)
            });
        inventory
            .Setup(service => service.GetAvailableQuantityAsync(productId, branchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var service = new ProductCatalogServiceImplementation(products.Object, branches.Object, inventory.Object);

        var catalog = await service.GetCatalogAsync(branchId);

        var product = Assert.Single(catalog);
        Assert.Equal(productId, product.ProductId);
        Assert.Equal("SKU-ONLINE", product.Sku);
        Assert.Equal(29m, product.SalePrice);
        Assert.Equal(branchId, product.BranchId);
        Assert.Equal(7, product.AvailableQuantity);
    }

    [Fact]
    public async Task Catalog_Treats_Empty_Branch_As_Unscoped_Catalog()
    {
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var branches = new Mock<IBranchRepository>(MockBehavior.Strict);
        var inventory = new Mock<IInventoryService>(MockBehavior.Strict);
        var productId = Guid.NewGuid();

        products
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ProductDto(productId, "Store product", "SKU-ONLINE", 12m, 29m, null, null)
            });

        var service = new ProductCatalogServiceImplementation(products.Object, branches.Object, inventory.Object);

        var catalog = await service.GetCatalogAsync(Guid.Empty);

        var product = Assert.Single(catalog);
        Assert.Null(product.BranchId);
        Assert.Null(product.AvailableQuantity);
        branches.VerifyNoOtherCalls();
        inventory.VerifyNoOtherCalls();
    }
}
