using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mercato.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return BadRequest();
        try { await _authService.RegisterAsync(request.Email, request.Password, cancellationToken); return Ok(); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return BadRequest();
        try
        {
            var user = await _authService.LoginAsync(request.Email, request.Password, cancellationToken);
            var token = CreateToken(user);
            return Ok(new { token, user = new { user.Id, user.Email, user.Username, user.Role, user.CanAccessBackOffice } });
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
    }

    private string CreateToken(AuthenticatedUser user)
    {
        var jwt = _configuration.GetSection("Jwt");
        var issuer = jwt["Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
        var audience = jwt["Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");
        var key = jwt["Key"] ?? throw new InvalidOperationException("JWT signing key is not configured.");
        if (key.Length < 32) throw new InvalidOperationException("JWT signing key must be at least 32 characters.");

        var displayIdentity = string.IsNullOrWhiteSpace(user.Username) ? user.Email : user.Username;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString("D")),
            new(ClaimTypes.NameIdentifier, user.Id.ToString("D")),
            new(ClaimTypes.Name, displayIdentity),
            new(ClaimTypes.Role, user.Role),
            new("backoffice", user.CanAccessBackOffice ? "true" : "false")
        };
        if (!string.IsNullOrWhiteSpace(user.Email)) claims.Add(new Claim(ClaimTypes.Email, user.Email));
        if (!string.IsNullOrWhiteSpace(user.Username)) claims.Add(new Claim("username", user.Username));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.AddHours(12), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Email remains the JSON property name for compatibility. It carries the configured login identifier.
    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string Email, string Password);
}
