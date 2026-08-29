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

public class ProductReferenceTests
{
    [Fact]
    public async Task Create_Rejects_Unknown_Category_Before_Product_Write()
    {
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var categories = new Mock<ICategoryRepository>(MockBehavior.Strict);
        var artists = new Mock<IArtistRepository>(MockBehavior.Strict);
        var categoryId = Guid.NewGuid();

        categories
            .Setup(repository => repository.GetAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var service = new ProductServiceImplementation(products.Object, categories.Object, artists.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CreateProductRequest("Product", "SKU-1", 5m, 10m, categoryId, null)));

        Assert.Equal("Category was not found.", exception.Message);
        products.Verify(repository => repository.AddAsync(It.IsAny<ProductDto>(), It.IsAny<CancellationToken>()), Times.Never);
        artists.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_Rejects_Unknown_Artist_Before_Product_Write()
    {
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var categories = new Mock<ICategoryRepository>(MockBehavior.Strict);
        var artists = new Mock<IArtistRepository>(MockBehavior.Strict);
        var artistId = Guid.NewGuid();

        artists
            .Setup(repository => repository.GetAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Artist?)null);

        var service = new ProductServiceImplementation(products.Object, categories.Object, artists.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CreateProductRequest("Product", "SKU-1", 5m, 10m, null, artistId)));

        Assert.Equal("Artist was not found.", exception.Message);
        products.Verify(repository => repository.AddAsync(It.IsAny<ProductDto>(), It.IsAny<CancellationToken>()), Times.Never);
        categories.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_Persists_Validated_Category_And_Artist_References()
    {
        var products = new Mock<IProductRepository>(MockBehavior.Strict);
        var categories = new Mock<ICategoryRepository>(MockBehavior.Strict);
        var artists = new Mock<IArtistRepository>(MockBehavior.Strict);
        var categoryId = Guid.NewGuid();
        var artistId = Guid.NewGuid();

        categories
            .Setup(repository => repository.GetAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = categoryId, Name = "Category" });
        artists
            .Setup(repository => repository.GetAsync(artistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Artist { Id = artistId, Name = "Artist" });
        products
            .Setup(repository => repository.AddAsync(It.IsAny<ProductDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto product, CancellationToken _) => product);

        var service = new ProductServiceImplementation(products.Object, categories.Object, artists.Object);

        var result = await service.CreateAsync(
            new CreateProductRequest("  Product  ", "  SKU-1  ", 5m, 10m, categoryId, artistId));

        Assert.Equal("Product", result.Name);
        Assert.Equal("SKU-1", result.Sku);
        Assert.Equal(categoryId, result.CategoryId);
        Assert.Equal(artistId, result.ArtistId);
        products.Verify(repository => repository.AddAsync(
            It.Is<ProductDto>(product => product.CategoryId == categoryId && product.ArtistId == artistId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
