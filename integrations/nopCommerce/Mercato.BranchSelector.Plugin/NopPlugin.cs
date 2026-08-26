using Nop.Services.Cms;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Mercato.BranchSelector.Plugin;

public sealed class MercatoBranchSelectorPlugin : BasePlugin, IWidgetPlugin
{
    public bool HideInWidgetList => false;

    public Task<IList<string>> GetWidgetZonesAsync()
        => Task.FromResult<IList<string>>([PublicWidgetZones.HeaderSelectors]);

    public Type GetWidgetViewComponent(string widgetZone)
        => typeof(BranchSelectorViewComponent);
}
