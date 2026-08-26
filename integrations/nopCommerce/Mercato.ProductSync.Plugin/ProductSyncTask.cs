using Nop.Services.ScheduleTasks;

namespace Mercato.ProductSync.Plugin;

public sealed class ProductSyncTask : IScheduleTask
{
    private readonly ProductSyncCore _sync;

    public ProductSyncTask(ProductSyncCore sync)
    {
        _sync = sync;
    }

    public Task ExecuteAsync() => _sync.SyncAsync();
}
