using Nop.Core.Domain.Cms;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Mercato.BranchSelector.Plugin;

public sealed class MercatoBranchSelectorPlugin : BasePlugin, IWidgetPlugin
{
    private readonly ISettingService _settingService;
    private readonly WidgetSettings _widgetSettings;

    public MercatoBranchSelectorPlugin(
        ISettingService settingService,
        WidgetSettings widgetSettings)
    {
        _settingService = settingService;
        _widgetSettings = widgetSettings;
    }

    public bool HideInWidgetList => false;

    public Task<IList<string>> GetWidgetZonesAsync()
        => Task.FromResult<IList<string>>([PublicWidgetZones.HeaderSelectors]);

    public Type GetWidgetViewComponent(string widgetZone)
        => typeof(BranchSelectorViewComponent);

    public override async Task InstallAsync()
    {
        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(PluginDescriptor.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(PluginDescriptor.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        if (_widgetSettings.ActiveWidgetSystemNames.Contains(PluginDescriptor.SystemName))
        {
            _widgetSettings.ActiveWidgetSystemNames.Remove(PluginDescriptor.SystemName);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        await base.UninstallAsync();
    }
}
