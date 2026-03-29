namespace TaskRunner.Core.Operations;

/// <summary>
/// Represents the result of a synchronous operation.
/// </summary>
public class OperationResult
{
    /// <summary>
    /// Whether the operation completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error details if the operation failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Operation-specific result data.
    /// </summary>
    public Dictionary<string, object?>? Data { get; set; }

    public static OperationResult Successful(string? message = null, Dictionary<string, object?>? data = null)
    {
        return new OperationResult { Success = true, Message = message, Data = data };
    }

    public static OperationResult Failed(string error)
    {
        return new OperationResult { Success = false, Error = error };
    }
}
