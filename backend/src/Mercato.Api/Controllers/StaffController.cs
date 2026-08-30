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
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "Manager", "Cashier"
    };

    private readonly IUserRepository _users;
    private readonly PasswordService _passwords;

    public StaffController(IUserRepository users, PasswordService passwords)
    {
        _users = users;
        _passwords = passwords;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _users.GetAllAsync(cancellationToken);
        return Ok(users.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email)) return BadRequest(new { error = "Email is required." });
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return BadRequest(new { error = "Password must be at least 8 characters." });
        if (!TryNormalizeRole(request.Role, out var role))
            return BadRequest(new { error = "Role must be Admin, Manager, or Cashier." });
        if (await _users.GetByEmailAsync(email, cancellationToken) is not null)
            return Conflict(new { error = "Email already exists." });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _passwords.Hash(request.Password),
            Role = role
        };
        await _users.AddAsync(user, cancellationToken);
        return Ok(ToDto(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateStaffRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.GetAsync(id, cancellationToken);
        if (user is null) return NotFound();

        if (!TryNormalizeRole(request.Role, out var role))
            return BadRequest(new { error = "Role must be Admin, Manager, or Cashier." });

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(currentUserId, out var currentId) && currentId == id && role != "Admin")
            return BadRequest(new { error = "You cannot remove your own Admin role." });

        user.Role = role;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 8)
                return BadRequest(new { error = "Password must be at least 8 characters." });
            user.PasswordHash = _passwords.Hash(request.Password);
        }

        await _users.UpdateAsync(user, cancellationToken);
        return Ok(ToDto(user));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(currentUserId, out var currentId) && currentId == id)
            return BadRequest(new { error = "You cannot delete your own account." });

        return await _users.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    private static object ToDto(User user) => new { user.Id, user.Email, user.Role };

    private static bool TryNormalizeRole(string? value, out string role)
    {
        role = string.Empty;
        if (string.IsNullOrWhiteSpace(value) || !AllowedRoles.Contains(value.Trim())) return false;
        role = AllowedRoles.First(x => x.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
        return true;
    }

    public sealed record CreateStaffRequest(string Email, string Password, string Role);
    public sealed record UpdateStaffRequest(string Role, string? Password);
}
