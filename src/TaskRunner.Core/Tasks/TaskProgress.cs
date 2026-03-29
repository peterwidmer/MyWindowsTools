namespace TaskRunner.Core.Tasks;

/// <summary>
/// Interface for reporting task progress to the frontend.
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// Reports progress with a structured status update.
    /// </summary>
    /// <param name="progress">The progress information.</param>
    Task ReportProgressAsync(TaskProgress progress);
}

/// <summary>
/// Represents progress information for a running task.
/// </summary>
public class TaskProgress
{
    /// <summary>
    /// Overall percentage complete (0-100). Null if indeterminate.
    /// </summary>
    public int? PercentComplete { get; set; }

    /// <summary>
    /// Current phase or step name.
    /// </summary>
    public string? CurrentPhase { get; set; }

    /// <summary>
    /// Detailed status message.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Current item being processed (e.g., file name, record number).
    /// </summary>
    public string? CurrentItem { get; set; }

    /// <summary>
    /// Total number of items to process.
    /// </summary>
    public int? TotalItems { get; set; }

    /// <summary>
    /// Number of items processed so far.
    /// </summary>
    public int? ProcessedItems { get; set; }

    /// <summary>
    /// Estimated time remaining in seconds.
    /// </summary>
    public double? EstimatedSecondsRemaining { get; set; }

    /// <summary>
    /// Custom data specific to the task type.
    /// </summary>
    public Dictionary<string, object?>? CustomData { get; set; }
}
