using Mercato.Application.Interfaces;

namespace Mercato.Application.Services;

public sealed class AuthService : IAuthService
{
    public Task<string> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(string.Empty);
    }
}
