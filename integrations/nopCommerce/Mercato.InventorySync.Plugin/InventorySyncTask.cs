using Microsoft.Extensions.Configuration;
using Nop.Services.ScheduleTasks;

namespace Mercato.InventorySync.Plugin;

public sealed class InventorySyncTask : IScheduleTask
{
    private readonly InventorySyncCore _sync;
    private readonly IConfiguration _configuration;

    public InventorySyncTask(InventorySyncCore sync, IConfiguration configuration)
    {
        _sync = sync;
        _configuration = configuration;
    }

    public Task ExecuteAsync()
    {
        var value = _configuration["Mercato:DefaultBranchId"];
        if (!Guid.TryParse(value, out var branchId) || branchId == Guid.Empty)
            throw new InvalidOperationException("Mercato:DefaultBranchId is required for scheduled inventory sync.");

        return _sync.SyncBranchAsync(branchId);
    }
}
