using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public interface IOrderService
{
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);
}
