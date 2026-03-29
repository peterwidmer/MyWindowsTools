namespace TaskRunner.Core.Operations.Implementations;

/// <summary>
/// Sample synchronous operation that returns system information.
/// </summary>
public class SystemInfoOperation : ISyncOperation
{
    public string OperationType => "system-info";
    public string DisplayName => "System Information";

    public Task<OperationResult> ExecuteAsync(Dictionary<string, object?> parameters)
    {
        var info = new Dictionary<string, object?>
        {
            ["machineName"] = Environment.MachineName,
            ["osVersion"] = Environment.OSVersion.ToString(),
            ["processorCount"] = Environment.ProcessorCount,
            ["is64BitOperatingSystem"] = Environment.Is64BitOperatingSystem,
            ["is64BitProcess"] = Environment.Is64BitProcess,
            ["dotnetVersion"] = Environment.Version.ToString(),
            ["currentDirectory"] = Environment.CurrentDirectory,
            ["systemDirectory"] = Environment.SystemDirectory,
            ["userName"] = Environment.UserName,
            ["workingSet"] = Environment.WorkingSet,
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        return Task.FromResult(OperationResult.Successful(
            "System information retrieved successfully",
            info));
    }
}
