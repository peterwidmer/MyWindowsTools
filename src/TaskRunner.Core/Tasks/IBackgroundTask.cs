namespace TaskRunner.Core.Tasks;

/// <summary>
/// Base interface for all background tasks that can report progress.
/// </summary>
public interface IBackgroundTask
{
    /// <summary>
    /// Unique identifier for this task type.
    /// </summary>
    string TaskType { get; }

    /// <summary>
    /// Human-readable name for the task.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Executes the task with progress reporting.
    /// </summary>
    /// <param name="parameters">Task-specific parameters as a dictionary.</param>
    /// <param name="progressReporter">Reporter to send progress updates.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The result of the task execution.</returns>
    Task<TaskResult> ExecuteAsync(
        Dictionary<string, object?> parameters,
        IProgressReporter progressReporter,
        CancellationToken cancellationToken);
}
