using Microsoft.EntityFrameworkCore;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly MercatoDbContext _context;

    public OrderRepository(MercatoDbContext context)
    {
        _context = context;
    }

    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
    }
}
