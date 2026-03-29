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
    private readonly ConcurrentDictionary<string, TaskSnapshot> _taskSnapshots = new();

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
        var startTime = DateTime.UtcNow;

        _taskSnapshots[taskId] = new TaskSnapshot
        {
            TaskId = taskId,
            TaskType = taskType,
            Status = "running",
            StartTime = startTime
        };

        var progressReporter = new SignalRProgressReporter(_hubContext, taskId, progress =>
        {
            if (_taskSnapshots.TryGetValue(taskId, out var snapshot))
            {
                snapshot.Progress = progress;
            }

            return Task.CompletedTask;
        });

        var runningTask = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Starting task {TaskId} of type {TaskType}", taskId, taskType);
                
                await _hubContext.Clients.Group(taskId).SendAsync("TaskStarted", new
                {
                    TaskId = taskId,
                    TaskType = taskType,
                    StartTime = startTime
                });

                var result = await task.ExecuteAsync(parameters, progressReporter, cts.Token);

                if (_taskSnapshots.TryGetValue(taskId, out var completedSnapshot))
                {
                    completedSnapshot.Status = "completed";
                    completedSnapshot.Result = result;
                    completedSnapshot.EndTime = DateTime.UtcNow;
                }

                await _hubContext.Clients.Group(taskId).SendAsync("TaskCompleted", new
                {
                    TaskId = taskId,
                    Result = result,
                    EndTime = _taskSnapshots[taskId].EndTime
                });

                _logger.LogInformation("Task {TaskId} completed successfully", taskId);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Task {TaskId} was cancelled", taskId);

                if (_taskSnapshots.TryGetValue(taskId, out var cancelledSnapshot))
                {
                    cancelledSnapshot.Status = "cancelled";
                    cancelledSnapshot.EndTime = DateTime.UtcNow;
                }
                
                await _hubContext.Clients.Group(taskId).SendAsync("TaskCancelled", new
                {
                    TaskId = taskId,
                    EndTime = _taskSnapshots[taskId].EndTime
                });

                return TaskResult.Failed("Task was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task {TaskId} failed with error", taskId);

                if (_taskSnapshots.TryGetValue(taskId, out var failedSnapshot))
                {
                    failedSnapshot.Status = "failed";
                    failedSnapshot.Result = TaskResult.Failed(ex.Message);
                    failedSnapshot.EndTime = DateTime.UtcNow;
                }
                
                await _hubContext.Clients.Group(taskId).SendAsync("TaskFailed", new
                {
                    TaskId = taskId,
                    Error = ex.Message,
                    EndTime = _taskSnapshots[taskId].EndTime
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
            StartTime = startTime
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

    public TaskSnapshot? GetTaskSnapshot(string taskId)
    {
        return _taskSnapshots.TryGetValue(taskId, out var snapshot) ? snapshot : null;
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

public class TaskSnapshot
{
    public required string TaskId { get; init; }
    public required string TaskType { get; init; }
    public required string Status { get; set; }
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; set; }
    public TaskProgress? Progress { get; set; }
    public TaskResult? Result { get; set; }
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
