using Microsoft.AspNetCore.SignalR;
using TaskRunner.Core.Tasks;
using TaskRunner.Host.Services;

namespace TaskRunner.Host.Hubs;

/// <summary>
/// SignalR hub for real-time task progress communication.
/// </summary>
public class TaskHub : Hub
{
    private readonly ILogger<TaskHub> _logger;
    private readonly TaskExecutionService _taskExecutionService;

    public TaskHub(ILogger<TaskHub> logger, TaskExecutionService taskExecutionService)
    {
        _logger = logger;
        _taskExecutionService = taskExecutionService;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows clients to subscribe to updates for a specific task.
    /// </summary>
    public async Task SubscribeToTask(string taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, taskId);
        _logger.LogInformation("Client {ConnectionId} subscribed to task {TaskId}", Context.ConnectionId, taskId);

        var snapshot = _taskExecutionService.GetTaskSnapshot(taskId);
        if (snapshot is null)
        {
            return;
        }

        await Clients.Caller.SendAsync("TaskStarted", new
        {
            TaskId = snapshot.TaskId,
            TaskType = snapshot.TaskType,
            StartTime = snapshot.StartTime
        });

        if (snapshot.Progress is not null)
        {
            await Clients.Caller.SendAsync("TaskProgress", new
            {
                TaskId = snapshot.TaskId,
                Progress = snapshot.Progress
            });
        }

        switch (snapshot.Status)
        {
            case "completed" when snapshot.Result is not null:
                await Clients.Caller.SendAsync("TaskCompleted", new
                {
                    TaskId = snapshot.TaskId,
                    Result = snapshot.Result,
                    EndTime = snapshot.EndTime
                });
                break;
            case "cancelled":
                await Clients.Caller.SendAsync("TaskCancelled", new
                {
                    TaskId = snapshot.TaskId,
                    EndTime = snapshot.EndTime
                });
                break;
            case "failed":
                await Clients.Caller.SendAsync("TaskFailed", new
                {
                    TaskId = snapshot.TaskId,
                    Error = snapshot.Result?.Error ?? "Task failed",
                    EndTime = snapshot.EndTime
                });
                break;
        }
    }

    /// <summary>
    /// Allows clients to unsubscribe from task updates.
    /// </summary>
    public async Task UnsubscribeFromTask(string taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, taskId);
        _logger.LogInformation("Client {ConnectionId} unsubscribed from task {TaskId}", Context.ConnectionId, taskId);
    }
}
