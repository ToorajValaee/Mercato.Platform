using Mercato.Application.DTOs;

namespace Mercato.Application.Services;

public interface IOrderCheckoutService
{
    Task<CheckoutResult> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default);
}
