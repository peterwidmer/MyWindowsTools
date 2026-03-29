using TaskRunner.Core.Operations;

namespace TaskRunner.Host.Services;

/// <summary>
/// Service for executing synchronous operations.
/// </summary>
public class OperationExecutionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OperationExecutionService> _logger;

    public OperationExecutionService(
        IServiceProvider serviceProvider,
        ILogger<OperationExecutionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets all registered operation types.
    /// </summary>
    public IEnumerable<OperationTypeInfo> GetAvailableOperationTypes()
    {
        var operations = _serviceProvider.GetServices<ISyncOperation>();
        return operations.Select(o => new OperationTypeInfo
        {
            OperationType = o.OperationType,
            DisplayName = o.DisplayName
        });
    }

    /// <summary>
    /// Executes a synchronous operation.
    /// </summary>
    public async Task<OperationResult> ExecuteAsync(string operationType, Dictionary<string, object?> parameters)
    {
        var operation = _serviceProvider.GetServices<ISyncOperation>()
            .FirstOrDefault(o => o.OperationType == operationType);

        if (operation == null)
        {
            return OperationResult.Failed($"Unknown operation type: {operationType}");
        }

        try
        {
            _logger.LogInformation("Executing operation of type {OperationType}", operationType);
            var result = await operation.ExecuteAsync(parameters);
            _logger.LogInformation("Operation {OperationType} completed successfully", operationType);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation {OperationType} failed", operationType);
            return OperationResult.Failed(ex.Message);
        }
    }
}

public class OperationTypeInfo
{
    public required string OperationType { get; init; }
    public required string DisplayName { get; init; }
}
