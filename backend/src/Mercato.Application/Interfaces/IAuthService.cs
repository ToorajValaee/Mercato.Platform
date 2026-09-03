namespace Mercato.Application.Interfaces;

public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    string? Username,
    string Role,
    bool CanAccessBackOffice);

public interface IAuthService
{
    Task<AuthenticatedUser> LoginAsync(string identifier, string password, CancellationToken cancellationToken = default);
    Task RegisterAsync(string email, string password, CancellationToken cancellationToken = default);
}
