using Microsoft.EntityFrameworkCore;
using Mercato.Application.DTOs;
using Mercato.Application.Repositories;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly MercatoDbContext _context;

    public ProductRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid productId)
    {
        return await _context.Products.AnyAsync(
            x => x.Id == productId);
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Select(product => new ProductDto(
                product.Id,
                product.Name,
                product.PurchasePrice,
                product.SalePrice,
                product.CategoryId))
            .ToListAsync(cancellationToken);
    }
}
