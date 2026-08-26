using Nop.Core.Domain.Tasks;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;

namespace Mercato.InventorySync.Plugin;

public sealed class MercatoInventorySyncPlugin : BasePlugin
{
    private readonly IScheduleTaskService _scheduleTasks;

    public MercatoInventorySyncPlugin(IScheduleTaskService scheduleTasks)
    {
        _scheduleTasks = scheduleTasks;
    }

    private static string TaskType => $"{typeof(InventorySyncTask).FullName}, {typeof(InventorySyncTask).Assembly.GetName().Name}";

    public override async Task InstallAsync()
    {
        if (await _scheduleTasks.GetTaskByTypeAsync(TaskType) is null)
        {
            await _scheduleTasks.InsertTaskAsync(new ScheduleTask
            {
                Name = "Mercato inventory synchronization",
                Type = TaskType,
                Seconds = 300,
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
