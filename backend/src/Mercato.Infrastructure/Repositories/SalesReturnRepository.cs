using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class SalesReturnRepository : ISalesReturnRepository
{
    private readonly MercatoDbContext _context;

    public SalesReturnRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<SalesReturn> AddAsync(SalesReturn salesReturn, CancellationToken cancellationToken = default)
    {
        _context.SalesReturns.Add(salesReturn);
        await _context.SaveChangesAsync(cancellationToken);
        return salesReturn;
    }

    public Task<int> GetReturnedQuantityAsync(Guid orderId, Guid productId, CancellationToken cancellationToken = default)
    {
        return _context.SalesReturnLines
            .Where(x => x.ProductId == productId)
            .Where(x => _context.SalesReturns.Any(r => r.Id == x.SalesReturnId && r.OrderId == orderId))
            .SumAsync(x => x.Quantity, cancellationToken);
    }
}
