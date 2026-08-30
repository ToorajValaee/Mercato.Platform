using System.Security.Claims;
using Mercato.Application.Repositories;

namespace Mercato.Api.Services;

public sealed class CurrentUserBranchAccess
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _users;

    public CurrentUserBranchAccess(IHttpContextAccessor httpContextAccessor, IUserRepository users)
    {
        _httpContextAccessor = httpContextAccessor;
        _users = users;
    }

    public bool IsAdmin => string.Equals(
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role),
        "Admin",
        StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<Guid>> GetAllowedBranchIdsAsync(CancellationToken cancellationToken = default)
    {
        if (IsAdmin) return Array.Empty<Guid>();
        var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? await _users.GetBranchIdsAsync(userId, cancellationToken)
            : Array.Empty<Guid>();
    }

    public async Task<bool> CanAccessAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (IsAdmin) return true;
        var allowed = await GetAllowedBranchIdsAsync(cancellationToken);
        return allowed.Contains(branchId);
    }
}
