namespace Mercato.Application.Services;

public interface IOrderCheckoutService
{
    Task<object> CheckoutAsync(object request, CancellationToken cancellationToken = default);
}
