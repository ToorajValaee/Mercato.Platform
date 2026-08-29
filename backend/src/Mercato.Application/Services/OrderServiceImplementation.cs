using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class OrderServiceImplementation : IOrderService
{
    private readonly IOrderRepository _orders;

    public OrderServiceImplementation(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order.Id == Guid.Empty)
            order.Id = Guid.NewGuid();

        if (order.CreatedAtUtc == default)
            order.CreatedAtUtc = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            if (item.Id == Guid.Empty)
                item.Id = Guid.NewGuid();

            item.OrderId = order.Id;
        }

        return await _orders.AddAsync(order, cancellationToken);
    }

    public Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
            return Task.FromResult<Order?>(null);

        return _orders.GetAsync(orderId, cancellationToken);
    }
}
