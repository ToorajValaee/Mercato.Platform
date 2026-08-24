namespace Mercato.Application.Interfaces;

public interface IAuthService
{
    Task<string> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
}
