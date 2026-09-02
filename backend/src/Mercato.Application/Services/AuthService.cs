using Mercato.Application.Interfaces;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordService _passwordService;

    public AuthService(
        IUserRepository userRepository,
        PasswordService passwordService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
    }

    public async Task RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();
        var existing = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("Email already exists.");

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordService.Hash(password),
            Role = "User"
        };

        await _userRepository.AddAsync(user, cancellationToken);
    }

    public async Task<AuthenticatedUser> LoginAsync(string identifier, string password, CancellationToken cancellationToken = default)
    {
        var normalizedIdentifier = NormalizeIdentifier(identifier);
        var user = normalizedIdentifier.Contains('@')
            ? await _userRepository.GetByEmailAsync(normalizedIdentifier, cancellationToken)
            : await _userRepository.GetByMobileNumberAsync(normalizedIdentifier, cancellationToken);

        if (user is null || !_passwordService.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return new AuthenticatedUser(user.Id, user.Email, user.MobileNumber, user.Role, user.CanAccessBackOffice);
    }

    private static string NormalizeIdentifier(string value)
    {
        var chars = value.Trim().Select(ch => ch switch
        {
            >= '۰' and <= '۹' => (char)('0' + ch - '۰'),
            >= '٠' and <= '٩' => (char)('0' + ch - '٠'),
            _ => ch
        });
        return new string(chars.ToArray()).Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}
