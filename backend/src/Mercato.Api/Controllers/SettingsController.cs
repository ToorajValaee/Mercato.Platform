using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Admin,Manager,Cashier")]
public sealed class SettingsController : ControllerBase
{
    private readonly MercatoDbContext _db;
    private readonly IPaymentMethodRepository _paymentMethods;
    private readonly IDiscountRepository _discounts;

    public SettingsController(MercatoDbContext db, IPaymentMethodRepository paymentMethods, IDiscountRepository discounts)
    {
        _db = db;
        _paymentMethods = paymentMethods;
        _discounts = discounts;
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic(CancellationToken cancellationToken)
    {
        var settings = await _db.ApplicationSettings.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        return Ok(new
        {
            systemLanguage = settings.GetValueOrDefault("System.Language", "en"),
            posShowProductImages = bool.TryParse(settings.GetValueOrDefault("Pos.ShowProductImages"), out var show) && show,
            useUsername = bool.TryParse(settings.GetValueOrDefault("Auth.UseUsername"), out var useUsername) && useUsername,
            paymentMethods = await _paymentMethods.GetAllAsync(true, cancellationToken),
            discounts = await _discounts.GetAllAsync(true, cancellationToken)
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var settings = await _db.ApplicationSettings.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value, cancellationToken);
        return Ok(new
        {
            systemLanguage = settings.GetValueOrDefault("System.Language", "en"),
            posShowProductImages = bool.TryParse(settings.GetValueOrDefault("Pos.ShowProductImages"), out var show) && show,
            useUsername = bool.TryParse(settings.GetValueOrDefault("Auth.UseUsername"), out var useUsername) && useUsername,
            paymentMethods = await _paymentMethods.GetAllAsync(false, cancellationToken),
            discounts = await _discounts.GetAllAsync(false, cancellationToken)
        });
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSettings(UpdateApplicationSettingsRequest request, CancellationToken cancellationToken)
    {
        var language = request.SystemLanguage?.Trim().ToLowerInvariant();
        if (language is not ("en" or "fa")) return BadRequest(new { error = "System language must be en or fa." });

        var staff = await _db.Users.Where(x => x.Role == "Admin" || x.Role == "Manager" || x.Role == "Cashier").ToListAsync(cancellationToken);
        if (request.UseUsername)
        {
            foreach (var user in staff.Where(x => string.IsNullOrWhiteSpace(x.Username)))
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                    return BadRequest(new { error = "Every staff account needs a username before username login can be enabled." });
                user.Username = user.Email.Trim();
            }
            var duplicate = staff.Where(x => !string.IsNullOrWhiteSpace(x.Username))
                .GroupBy(x => x.Username!, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(x => x.Count() > 1);
            if (duplicate is not null)
                return BadRequest(new { error = $"Username '{duplicate.Key}' is duplicated. Resolve it before enabling username login." });
        }
        else if (staff.Any(x => string.IsNullOrWhiteSpace(x.Email)))
        {
            return BadRequest(new { error = "Every staff account needs an email before email login can be enabled." });
        }

        await UpsertAsync("System.Language", language, cancellationToken);
        await UpsertAsync("Pos.ShowProductImages", request.PosShowProductImages.ToString().ToLowerInvariant(), cancellationToken);
        await UpsertAsync("Auth.UseUsername", request.UseUsername.ToString().ToLowerInvariant(), cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("payment-methods")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePaymentMethod(PaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var error = ValidateName(request.Name); if (error is not null) return BadRequest(new { error });
        try { return Ok(await _paymentMethods.AddAsync(new PaymentMethod { Id = Guid.NewGuid(), Name = request.Name.Trim(), IsActive = request.IsActive, SortOrder = request.SortOrder }, cancellationToken)); }
        catch (DbUpdateException) { return Conflict(new { error = "A payment method with this name already exists." }); }
    }

    [HttpPut("payment-methods/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePaymentMethod(Guid id, PaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var error = ValidateName(request.Name); if (error is not null) return BadRequest(new { error });
        try { var updated = await _paymentMethods.UpdateAsync(new PaymentMethod { Id = id, Name = request.Name.Trim(), IsActive = request.IsActive, SortOrder = request.SortOrder }, cancellationToken); return updated is null ? NotFound() : Ok(updated); }
        catch (DbUpdateException) { return Conflict(new { error = "A payment method with this name already exists." }); }
    }

    [HttpDelete("payment-methods/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePaymentMethod(Guid id, CancellationToken cancellationToken)
        => await _paymentMethods.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpPost("discounts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDiscount(DiscountRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateDiscount(request); if (validation is not null) return BadRequest(new { error = validation });
        try { return Ok(await _discounts.AddAsync(new DiscountDefinition { Id = Guid.NewGuid(), Name = request.Name.Trim(), Type = NormalizeDiscountType(request.Type), Value = request.Value, IsActive = request.IsActive, SortOrder = request.SortOrder }, cancellationToken)); }
        catch (DbUpdateException) { return Conflict(new { error = "A discount with this name already exists." }); }
    }

    [HttpPut("discounts/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDiscount(Guid id, DiscountRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateDiscount(request); if (validation is not null) return BadRequest(new { error = validation });
        try { var updated = await _discounts.UpdateAsync(new DiscountDefinition { Id = id, Name = request.Name.Trim(), Type = NormalizeDiscountType(request.Type), Value = request.Value, IsActive = request.IsActive, SortOrder = request.SortOrder }, cancellationToken); return updated is null ? NotFound() : Ok(updated); }
        catch (DbUpdateException) { return Conflict(new { error = "A discount with this name already exists." }); }
    }

    [HttpDelete("discounts/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDiscount(Guid id, CancellationToken cancellationToken)
        => await _discounts.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    private async Task UpsertAsync(string key, string value, CancellationToken cancellationToken)
    {
        var row = await _db.ApplicationSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (row is null) _db.ApplicationSettings.Add(new ApplicationSetting { Key = key, Value = value }); else row.Value = value;
    }
    private static string? ValidateName(string? name) => string.IsNullOrWhiteSpace(name) ? "Name is required." : name.Trim().Length > 100 ? "Name is too long." : null;
    private static string? ValidateDiscount(DiscountRequest request)
    {
        var nameError = ValidateName(request.Name); if (nameError is not null) return nameError;
        var type = NormalizeDiscountType(request.Type); if (type is not ("Percent" or "Fixed")) return "Discount type must be Percent or Fixed.";
        if (request.Value <= 0) return "Discount value must be greater than zero.";
        if (type == "Percent" && request.Value > 100) return "Percent discount cannot exceed 100.";
        return null;
    }
    private static string NormalizeDiscountType(string? type) => type?.Trim().Equals("fixed", StringComparison.OrdinalIgnoreCase) == true ? "Fixed" : type?.Trim().Equals("percent", StringComparison.OrdinalIgnoreCase) == true ? "Percent" : type?.Trim() ?? string.Empty;

    public sealed record UpdateApplicationSettingsRequest(string SystemLanguage, bool PosShowProductImages, bool UseUsername = false);
    public sealed record PaymentMethodRequest(string Name, bool IsActive, int SortOrder);
    public sealed record DiscountRequest(string Name, string Type, decimal Value, bool IsActive, int SortOrder);
}
