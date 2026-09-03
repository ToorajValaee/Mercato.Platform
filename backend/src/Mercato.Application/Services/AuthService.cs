using Mercato.Application.Interfaces;
using Mercato.Application.Repositories;
using Mercato.Domain.Entities;

namespace Mercato.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IApplicationSettingRepository _settings;
    private readonly PasswordService _passwordService;

    public AuthService(
        IUserRepository userRepository,
        IApplicationSettingRepository settings,
        PasswordService passwordService)
    {
        _userRepository = userRepository;
        _settings = settings;
        _passwordService = passwordService;
    }

    // Compatibility overload for existing unit tests and direct application callers.
    // Production DI resolves the three-argument constructor and therefore uses the
    // persisted Auth.UseUsername setting.
    public AuthService(IUserRepository userRepository, PasswordService passwordService)
        : this(userRepository, new EmailModeSettings(), passwordService) { }

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
        var normalized = identifier.Trim();
        var useUsername = await _settings.GetBooleanAsync("Auth.UseUsername", false, cancellationToken);
        var user = useUsername
            ? await _userRepository.GetByUsernameAsync(normalized, cancellationToken)
            : await _userRepository.GetByEmailAsync(normalized, cancellationToken);

        if (user is null || !_passwordService.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return new AuthenticatedUser(user.Id, user.Email, user.Username, user.Role, user.CanAccessBackOffice);
    }

    private sealed class EmailModeSettings : IApplicationSettingRepository
    {
        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> GetBooleanAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default) => Task.FromResult(defaultValue);
    }
}
