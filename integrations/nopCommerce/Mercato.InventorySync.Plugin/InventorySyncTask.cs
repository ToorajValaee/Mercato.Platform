using Mercato.NopCommerce.Core;
using Nop.Services.ScheduleTasks;

namespace Mercato.InventorySync.Plugin;

public sealed class InventorySyncTask : IScheduleTask
{
    private readonly InventorySyncCore _sync;
    private readonly IMercatoConfiguration _configuration;

    public InventorySyncTask(
        InventorySyncCore sync,
        IMercatoConfiguration configuration)
    {
        _sync = sync;
        _configuration = configuration;
    }

    public Task ExecuteAsync()
    {
        var branchId = _configuration.DefaultBranchId;
        if (branchId is null || branchId == Guid.Empty)
            throw new InvalidOperationException("A default Mercato branch is required for scheduled inventory sync.");

        return _sync.SyncBranchAsync(branchId.Value);
    }
}
