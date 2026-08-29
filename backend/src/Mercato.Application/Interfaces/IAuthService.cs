namespace Mercato.Application.Interfaces;

public sealed record AuthenticatedUser(Guid Id, string Email, string Role);

public interface IAuthService
{
    Task<AuthenticatedUser> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task RegisterAsync(string email, string password, CancellationToken cancellationToken = default);
}
