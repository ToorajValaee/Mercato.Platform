using System.Security.Claims;
using Mercato.Application.Repositories;
using Mercato.Application.Services;
using Mercato.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "Admin")]
public sealed class StaffController : ControllerBase
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase) { "Admin", "Manager", "Cashier" };
    private readonly IUserRepository _users;
    private readonly IBranchRepository _branches;
    private readonly IApplicationSettingRepository _settings;
    private readonly PasswordService _passwords;

    public StaffController(IUserRepository users, IBranchRepository branches, IApplicationSettingRepository settings, PasswordService passwords)
    {
        _users = users;
        _branches = branches;
        _settings = settings;
        _passwords = passwords;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _users.GetAllAsync(cancellationToken);
        var result = new List<object>(users.Count);
        foreach (var user in users) result.Add(await ToDtoAsync(user, cancellationToken));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var useUsername = await _settings.GetBooleanAsync("Auth.UseUsername", false, cancellationToken);
        var username = Normalize(request.Username);
        var email = Normalize(request.Email);
        var identityError = await ValidateIdentityAsync(null, useUsername, username, email, cancellationToken);
        if (identityError is not null) return identityError;
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return BadRequest(new { error = "Password must be at least 8 characters." });
        if (!TryNormalizeRole(request.Role, out var role))
            return BadRequest(new { error = "Role must be Admin, Manager, or Cashier." });

        var branchIds = await ValidateBranchIdsAsync(request.BranchIds, cancellationToken);
        if (branchIds is null) return BadRequest(new { error = "One or more selected branches do not exist." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email ?? string.Empty,
            Username = username,
            MobileNumber = null,
            PasswordHash = _passwords.Hash(request.Password),
            Role = role,
            CanAccessBackOffice = request.CanAccessBackOffice
        };
        await _users.AddAsync(user, cancellationToken);
        await _users.SetBranchIdsAsync(user.Id, branchIds, cancellationToken);
        return Ok(await ToDtoAsync(user, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateStaffRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.GetAsync(id, cancellationToken);
        if (user is null) return NotFound();
        var useUsername = await _settings.GetBooleanAsync("Auth.UseUsername", false, cancellationToken);
        var username = Normalize(request.Username);
        var email = Normalize(request.Email);
        var identityError = await ValidateIdentityAsync(id, useUsername, username, email, cancellationToken);
        if (identityError is not null) return identityError;
        if (!TryNormalizeRole(request.Role, out var role))
            return BadRequest(new { error = "Role must be Admin, Manager, or Cashier." });

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(currentUserId, out var currentId) && currentId == id)
        {
            if (role != "Admin") return BadRequest(new { error = "You cannot remove your own Admin role." });
            if (!request.CanAccessBackOffice) return BadRequest(new { error = "You cannot remove your own Back Office access." });
        }

        var branchIds = await ValidateBranchIdsAsync(request.BranchIds, cancellationToken);
        if (branchIds is null) return BadRequest(new { error = "One or more selected branches do not exist." });

        user.Username = username;
        user.Email = email ?? string.Empty;
        user.MobileNumber = null;
        user.Role = role;
        user.CanAccessBackOffice = request.CanAccessBackOffice;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 8) return BadRequest(new { error = "Password must be at least 8 characters." });
            user.PasswordHash = _passwords.Hash(request.Password);
        }
        await _users.UpdateAsync(user, cancellationToken);
        await _users.SetBranchIdsAsync(user.Id, branchIds, cancellationToken);
        return Ok(await ToDtoAsync(user, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(currentUserId, out var currentId) && currentId == id)
            return BadRequest(new { error = "You cannot delete your own account." });
        return await _users.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    private async Task<IActionResult?> ValidateIdentityAsync(Guid? currentId, bool useUsername, string? username, string? email, CancellationToken cancellationToken)
    {
        if (useUsername && string.IsNullOrWhiteSpace(username)) return BadRequest(new { error = "Username is required while username login is enabled." });
        if (!useUsername && string.IsNullOrWhiteSpace(email)) return BadRequest(new { error = "Email is required while username login is disabled." });
        if (!string.IsNullOrWhiteSpace(username))
        {
            var duplicate = await _users.GetByUsernameAsync(username, cancellationToken);
            if (duplicate is not null && duplicate.Id != currentId) return Conflict(new { error = "Username already exists." });
        }
        if (!string.IsNullOrWhiteSpace(email))
        {
            var duplicate = await _users.GetByEmailAsync(email, cancellationToken);
            if (duplicate is not null && duplicate.Id != currentId) return Conflict(new { error = "Email already exists." });
        }
        return null;
    }

    private async Task<object> ToDtoAsync(User user, CancellationToken cancellationToken) => new
    {
        user.Id,
        user.Username,
        user.Email,
        user.Role,
        user.CanAccessBackOffice,
        BranchIds = await _users.GetBranchIdsAsync(user.Id, cancellationToken)
    };

    private async Task<IReadOnlyList<Guid>?> ValidateBranchIdsAsync(IReadOnlyCollection<Guid>? values, CancellationToken cancellationToken)
    {
        var ids = (values ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToArray();
        foreach (var id in ids) if (await _branches.GetAsync(id, cancellationToken) is null) return null;
        return ids;
    }

    private static bool TryNormalizeRole(string? value, out string role)
    {
        role = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || !AllowedRoles.Contains(value.Trim())) return false;
        role = AllowedRoles.First(x => x.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
        return true;
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed record CreateStaffRequest(string? Username, string? Email, string Password, string Role, bool CanAccessBackOffice = false, IReadOnlyCollection<Guid>? BranchIds = null);
    public sealed record UpdateStaffRequest(string? Username, string? Email, string Role, bool CanAccessBackOffice, string? Password, IReadOnlyCollection<Guid>? BranchIds = null);
}
