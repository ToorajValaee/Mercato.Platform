using Mercato.Domain.Entities;

namespace Mercato.Application.Repositories;

public interface IOrderRepository
{
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
}
