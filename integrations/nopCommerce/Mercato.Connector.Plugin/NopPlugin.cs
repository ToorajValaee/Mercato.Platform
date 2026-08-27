using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Plugins;

namespace Mercato.Connector.Plugin;

public sealed class MercatoConnectorPlugin : BasePlugin, IMiscPlugin
{
    private readonly IWebHelper _webHelper;
    private readonly ISettingService _settings;

    public MercatoConnectorPlugin(
        IWebHelper webHelper,
        ISettingService settings)
    {
        _webHelper = webHelper;
        _settings = settings;
    }

    public override string GetConfigurationPageUrl()
        => $"{_webHelper.GetStoreLocation()}Admin/MercatoConnector/Configure";

    public override async Task UninstallAsync()
    {
        await _settings.DeleteSettingAsync<MercatoConnectorSettings>();
        await base.UninstallAsync();
    }
}
