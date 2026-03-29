namespace TaskRunner.Core.Operations;

/// <summary>
/// Base interface for simple synchronous operations.
/// </summary>
public interface ISyncOperation
{
    /// <summary>
    /// Unique identifier for this operation type.
    /// </summary>
    string OperationType { get; }

    /// <summary>
    /// Human-readable name for the operation.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Executes the operation synchronously.
    /// </summary>
    /// <param name="parameters">Operation-specific parameters.</param>
    /// <returns>The result of the operation.</returns>
    Task<OperationResult> ExecuteAsync(Dictionary<string, object?> parameters);
}
