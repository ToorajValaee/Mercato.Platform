using Microsoft.AspNetCore.Mvc;
using Nop.Services.Configuration;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Mercato.Connector.Plugin;

[Area(AreaNames.ADMIN)]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
public sealed class MercatoConnectorController : BasePluginController
{
    private const string ViewPath = "~/Plugins/Mercato.Connector/Views/Configure.cshtml";
    private readonly ISettingService _settings;

    public MercatoConnectorController(ISettingService settings)
    {
        _settings = settings;
    }

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Configure()
    {
        var settings = await _settings.LoadSettingAsync<MercatoConnectorSettings>();
        return View(ViewPath, ToModel(settings));
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Configure(MercatoConnectorConfigurationModel model)
    {
        Validate(model);
        if (!ModelState.IsValid)
            return View(ViewPath, model);

        await _settings.SaveSettingAsync(new MercatoConnectorSettings
        {
            BaseUrl = model.BaseUrl.Trim(),
            BearerToken = model.BearerToken.Trim(),
            DefaultBranchId = model.DefaultBranchId.Trim()
        });

        TempData["MercatoConnectorSettingsSaved"] = true;
        return RedirectToAction(nameof(Configure));
    }

    private void Validate(MercatoConnectorConfigurationModel model)
    {
        if (!Uri.TryCreate(model.BaseUrl?.Trim(), UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            ModelState.AddModelError(nameof(model.BaseUrl), "Enter a valid absolute HTTP or HTTPS Mercato API URL.");
        }

        if (!string.IsNullOrWhiteSpace(model.DefaultBranchId) &&
            (!Guid.TryParse(model.DefaultBranchId, out var branchId) || branchId == Guid.Empty))
        {
            ModelState.AddModelError(nameof(model.DefaultBranchId), "Default branch must be a valid Mercato branch GUID.");
        }
    }

    private static MercatoConnectorConfigurationModel ToModel(MercatoConnectorSettings settings)
        => new()
        {
            BaseUrl = settings.BaseUrl,
            BearerToken = settings.BearerToken,
            DefaultBranchId = settings.DefaultBranchId
        };
}
