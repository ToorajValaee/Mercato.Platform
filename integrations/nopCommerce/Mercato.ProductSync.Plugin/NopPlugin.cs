using Nop.Core.Domain.ScheduleTasks;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;

namespace Mercato.ProductSync.Plugin;

public sealed class MercatoProductSyncPlugin : BasePlugin
{
    private readonly IScheduleTaskService _scheduleTasks;

    public MercatoProductSyncPlugin(IScheduleTaskService scheduleTasks)
    {
        _scheduleTasks = scheduleTasks;
    }

    private static string TaskType => $"{typeof(ProductSyncTask).FullName}, {typeof(ProductSyncTask).Assembly.GetName().Name}";

    public override async Task InstallAsync()
    {
        if (await _scheduleTasks.GetTaskByTypeAsync(TaskType) is null)
        {
            await _scheduleTasks.InsertTaskAsync(new ScheduleTask
            {
                Name = "Mercato product synchronization",
                Type = TaskType,
                Seconds = 900,
                Enabled = true,
                StopOnError = false,
                LastEnabledUtc = DateTime.UtcNow
            });
        }

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        var task = await _scheduleTasks.GetTaskByTypeAsync(TaskType);
        if (task is not null)
            await _scheduleTasks.DeleteTaskAsync(task);

        await base.UninstallAsync();
    }
}
