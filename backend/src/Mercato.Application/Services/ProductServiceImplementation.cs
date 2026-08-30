using Mercato.Application.DTOs;
using Mercato.Application.Repositories;

namespace Mercato.Application.Services;

public sealed class ProductServiceImplementation : IProductService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IArtistRepository _artists;

    public ProductServiceImplementation(
        IProductRepository products,
        ICategoryRepository categories,
        IArtistRepository artists)
    {
        _products = products;
        _categories = categories;
        _artists = artists;
    }

    public async Task<int> GetProductCountAsync(CancellationToken cancellationToken = default)
        => (await _products.GetAllAsync(cancellationToken)).Count;

    public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken = default)
        => _products.GetAllAsync(cancellationToken);

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request.Name, request.PurchasePrice, request.SalePrice, request.CategoryId, request.ArtistId, cancellationToken);
        var product = new ProductDto(
            Guid.NewGuid(),
            request.Name.Trim(),
            Normalize(request.Sku),
            Normalize(request.ImageUrl),
            request.PurchasePrice,
            request.SalePrice,
            request.CategoryId,
            request.ArtistId);
        return await _products.AddAsync(product, cancellationToken);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request.Name, request.PurchasePrice, request.SalePrice, request.CategoryId, request.ArtistId, cancellationToken);
        return await _products.UpdateAsync(id, request with
        {
            Name = request.Name.Trim(),
            Sku = Normalize(request.Sku),
            ImageUrl = Normalize(request.ImageUrl)
        }, cancellationToken);
    }

    public Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
        => _products.ArchiveAsync(id, cancellationToken);

    private async Task ValidateAsync(
        string name,
        decimal purchasePrice,
        decimal salePrice,
        Guid? categoryId,
        Guid? artistId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name is required.", nameof(name));
        if (purchasePrice < 0) throw new ArgumentOutOfRangeException(nameof(purchasePrice));
        if (salePrice <= 0) throw new ArgumentOutOfRangeException(nameof(salePrice));
        if (categoryId is Guid category && category != Guid.Empty && await _categories.GetAsync(category, cancellationToken) is null)
            throw new InvalidOperationException("Category was not found.");
        if (artistId is Guid artist && artist != Guid.Empty && await _artists.GetAsync(artist, cancellationToken) is null)
            throw new InvalidOperationException("Artist was not found.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
