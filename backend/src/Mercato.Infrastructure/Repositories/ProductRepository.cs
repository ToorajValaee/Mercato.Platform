using Microsoft.EntityFrameworkCore;
using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
using Mercato.Infrastructure.Data;
using Mercato.Domain.Entities;

namespace Mercato.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly MercatoDbContext _context;

    public ProductRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(Guid productId)
        => _context.Products.AnyAsync(x => x.Id == productId);

    public async Task<ProductDto?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
        => await _context.Products.AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => Map(product))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Products.AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => Map(product))
            .ToListAsync(cancellationToken);

    public async Task<ProductDto> AddAsync(ProductDto product, CancellationToken cancellationToken = default)
    {
        var entity = new Product
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            ImageUrl = product.ImageUrl,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            CategoryId = product.CategoryId,
            ArtistId = product.ArtistId
        };
        _context.Products.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Name = request.Name;
        entity.Sku = request.Sku;
        entity.ImageUrl = request.ImageUrl;
        entity.PurchasePrice = request.PurchasePrice;
        entity.SalePrice = request.SalePrice;
        entity.CategoryId = request.CategoryId;
        entity.ArtistId = request.ArtistId;
        await _context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return false;
        _context.Products.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProductDto Map(Product product) => new(
        product.Id,
        product.Name,
        product.Sku,
        product.PurchasePrice,
        product.SalePrice,
        product.CategoryId,
        product.ArtistId)
    {
        ImageUrl = product.ImageUrl
    };
}
