using Microsoft.AspNetCore.SignalR;
using TaskRunner.Core.Tasks;
using TaskRunner.Host.Hubs;

namespace TaskRunner.Host.Services;

/// <summary>
/// Progress reporter that sends updates via SignalR.
/// </summary>
public class SignalRProgressReporter : IProgressReporter
{
    private readonly IHubContext<TaskHub> _hubContext;
    private readonly string _taskId;

    public SignalRProgressReporter(IHubContext<TaskHub> hubContext, string taskId)
    {
        _hubContext = hubContext;
        _taskId = taskId;
    }

    public async Task ReportProgressAsync(TaskProgress progress)
    {
        await _hubContext.Clients.Group(_taskId).SendAsync("TaskProgress", new
        {
            TaskId = _taskId,
            Progress = progress
        });
    }
}
