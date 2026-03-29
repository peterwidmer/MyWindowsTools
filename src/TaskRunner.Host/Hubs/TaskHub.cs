using Microsoft.AspNetCore.SignalR;
using TaskRunner.Core.Tasks;

namespace TaskRunner.Host.Hubs;

/// <summary>
/// SignalR hub for real-time task progress communication.
/// </summary>
public class TaskHub : Hub
{
    private readonly ILogger<TaskHub> _logger;

    public TaskHub(ILogger<TaskHub> logger)
    {
        _logger = logger;
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
