using Mercato.Application.DTOs;

namespace Mercato.Application.Services;

public interface IReturnService
{
    Task<ReturnResult> ReturnAsync(ReturnRequest request, CancellationToken cancellationToken = default);
}
