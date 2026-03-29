namespace TaskRunner.Core.Tasks;

/// <summary>
/// Represents the result of a completed background task.
/// </summary>
public class TaskResult
{
    /// <summary>
    /// Whether the task completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error details if the task failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Task-specific result data.
    /// </summary>
    public Dictionary<string, object?>? Data { get; set; }

    public static TaskResult Successful(string? message = null, Dictionary<string, object?>? data = null)
    {
        return new TaskResult { Success = true, Message = message, Data = data };
    }

    public static TaskResult Failed(string error)
    {
        return new TaskResult { Success = false, Error = error };
    }
}
