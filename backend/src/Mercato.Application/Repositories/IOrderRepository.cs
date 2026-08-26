using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IOrderRepository
{
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetAsync(Guid orderId, CancellationToken cancellationToken = default);
}
