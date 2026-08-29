using Mercato.Application.DTOs;

namespace Mercato.Application.Services;

public interface IReturnService
{
    Task<ReturnableOrderDto?> GetReturnableOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ReturnResult> ReturnAsync(ReturnRequest request, CancellationToken cancellationToken = default);
}
