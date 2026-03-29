using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using TaskRunner.Core.Tasks;
using TaskRunner.Host.Hubs;

namespace TaskRunner.Host.Services;

/// <summary>
/// Service for managing and executing background tasks.
/// </summary>
public class TaskExecutionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<TaskHub> _hubContext;
    private readonly ILogger<TaskExecutionService> _logger;
    private readonly ConcurrentDictionary<string, RunningTaskInfo> _runningTasks = new();

    public TaskExecutionService(
        IServiceProvider serviceProvider,
        IHubContext<TaskHub> hubContext,
        ILogger<TaskExecutionService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets all registered background task types.
    /// </summary>
    public IEnumerable<TaskTypeInfo> GetAvailableTaskTypes()
    {
        var tasks = _serviceProvider.GetServices<IBackgroundTask>();
        return tasks.Select(t => new TaskTypeInfo
        {
            TaskType = t.TaskType,
            DisplayName = t.DisplayName
        });
    }

    /// <summary>
    /// Starts a new background task.
    /// </summary>
    public string StartTask(string taskType, Dictionary<string, object?> parameters)
    {
        var task = _serviceProvider.GetServices<IBackgroundTask>()
            .FirstOrDefault(t => t.TaskType == taskType);

        if (task == null)
        {
            throw new ArgumentException($"Unknown task type: {taskType}");
        }

        var taskId = Guid.NewGuid().ToString("N");
        var cts = new CancellationTokenSource();
        var progressReporter = new SignalRProgressReporter(_hubContext, taskId);

        var runningTask = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Starting task {TaskId} of type {TaskType}", taskId, taskType);
                
                await _hubContext.Clients.Group(taskId).SendAsync("TaskStarted", new
                {
                    TaskId = taskId,
                    TaskType = taskType,
                    StartTime = DateTime.UtcNow
                });

                var result = await task.ExecuteAsync(parameters, progressReporter, cts.Token);

                await _hubContext.Clients.Group(taskId).SendAsync("TaskCompleted", new
                {
                    TaskId = taskId,
                    Result = result,
                    EndTime = DateTime.UtcNow
                });

                _logger.LogInformation("Task {TaskId} completed successfully", taskId);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Task {TaskId} was cancelled", taskId);
                
                await _hubContext.Clients.Group(taskId).SendAsync("TaskCancelled", new
                {
                    TaskId = taskId,
                    EndTime = DateTime.UtcNow
                });

                return TaskResult.Failed("Task was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task {TaskId} failed with error", taskId);
                
                await _hubContext.Clients.Group(taskId).SendAsync("TaskFailed", new
                {
                    TaskId = taskId,
                    Error = ex.Message,
                    EndTime = DateTime.UtcNow
                });

                return TaskResult.Failed(ex.Message);
            }
            finally
            {
                _runningTasks.TryRemove(taskId, out _);
            }
        });

        _runningTasks[taskId] = new RunningTaskInfo
        {
            TaskId = taskId,
            TaskType = taskType,
            Task = runningTask,
            CancellationTokenSource = cts,
            StartTime = DateTime.UtcNow
        };

        return taskId;
    }

    /// <summary>
    /// Cancels a running task.
    /// </summary>
    public bool CancelTask(string taskId)
    {
        if (_runningTasks.TryGetValue(taskId, out var info))
        {
            info.CancellationTokenSource.Cancel();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets information about all running tasks.
    /// </summary>
    public IEnumerable<RunningTaskStatus> GetRunningTasks()
    {
        return _runningTasks.Values.Select(t => new RunningTaskStatus
        {
            TaskId = t.TaskId,
            TaskType = t.TaskType,
            StartTime = t.StartTime
        });
    }

    private class RunningTaskInfo
    {
        public required string TaskId { get; init; }
        public required string TaskType { get; init; }
        public required Task<TaskResult> Task { get; init; }
        public required CancellationTokenSource CancellationTokenSource { get; init; }
        public required DateTime StartTime { get; init; }
    }
}

public class TaskTypeInfo
{
    public required string TaskType { get; init; }
    public required string DisplayName { get; init; }
}

public class RunningTaskStatus
{
    public required string TaskId { get; init; }
    public required string TaskType { get; init; }
    public required DateTime StartTime { get; init; }
}
