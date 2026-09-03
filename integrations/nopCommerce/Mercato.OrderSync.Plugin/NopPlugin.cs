using Nop.Core.Domain.ScheduleTasks;
using Nop.Services.Common;
using Nop.Services.Plugins;
using Nop.Services.ScheduleTasks;

namespace Mercato.OrderSync.Plugin;

public sealed class MercatoOrderSyncPlugin : BasePlugin, IMiscPlugin
{
    private readonly IScheduleTaskService _scheduleTasks;

    public MercatoOrderSyncPlugin(IScheduleTaskService scheduleTasks)
    {
        _scheduleTasks = scheduleTasks;
    }

    private static string TaskType => $"{typeof(OrderSyncRetryTask).FullName}, {typeof(OrderSyncRetryTask).Assembly.GetName().Name}";

    public override async Task InstallAsync()
    {
        if (await _scheduleTasks.GetTaskByTypeAsync(TaskType) is null)
        {
            await _scheduleTasks.InsertTaskAsync(new ScheduleTask
            {
                Name = "Mercato paid order synchronization retry",
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
