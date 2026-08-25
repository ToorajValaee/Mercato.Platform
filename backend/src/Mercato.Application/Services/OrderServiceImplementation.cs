using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public interface IOrderService
{
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);
}

public sealed class OrderServiceImplementation : IOrderService
{
    public Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order.Id == Guid.Empty)
            order.Id = Guid.NewGuid();

        if (order.CreatedAtUtc == default)
            order.CreatedAtUtc = DateTime.UtcNow;

        return Task.FromResult(order);
    }
}
