using Mercato.NopCommerce.Core;
using Microsoft.Extensions.Configuration;
using Nop.Services.Configuration;

namespace Mercato.Connector.Plugin;

public sealed class NopMercatoConfiguration : IMercatoConfiguration
{
    public NopMercatoConfiguration(
        ISettingService settings,
        IConfiguration configuration)
    {
        var pluginSettings = settings.LoadSetting<MercatoConnectorSettings>();

        BaseUrl = !string.IsNullOrWhiteSpace(pluginSettings.BaseUrl)
            ? pluginSettings.BaseUrl.Trim()
            : configuration[MercatoNopDefaults.BaseUrlConfigurationKey]?.Trim() ?? string.Empty;

        BearerToken = !string.IsNullOrWhiteSpace(pluginSettings.BearerToken)
            ? pluginSettings.BearerToken.Trim()
            : configuration[MercatoNopDefaults.BearerTokenConfigurationKey]?.Trim() ?? string.Empty;

        var defaultBranchText = !string.IsNullOrWhiteSpace(pluginSettings.DefaultBranchId)
            ? pluginSettings.DefaultBranchId
            : configuration[MercatoNopDefaults.DefaultBranchIdConfigurationKey];

        DefaultBranchId = Guid.TryParse(defaultBranchText, out var branchId) && branchId != Guid.Empty
            ? branchId
            : null;
    }

    public string BaseUrl { get; }
    public string BearerToken { get; }
    public Guid? DefaultBranchId { get; }
}
