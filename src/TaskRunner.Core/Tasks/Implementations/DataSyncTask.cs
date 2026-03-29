using System.Text.Json;

namespace TaskRunner.Core.Tasks.Implementations;

/// <summary>
/// Sample long-running task that simulates data synchronization with multiple phases.
/// </summary>
public class DataSyncTask : IBackgroundTask
{
    public string TaskType => "data-sync";
    public string DisplayName => "Data Synchronization";

    public async Task<TaskResult> ExecuteAsync(
        Dictionary<string, object?> parameters,
        IProgressReporter progressReporter,
        CancellationToken cancellationToken)
    {
        var recordCount = TryGetInt(parameters, "recordCount", out var count) ? Math.Max(1, count) : 50;
        var syncedRecords = 0;
        var errors = 0;

        // Phase 1: Connection
        await progressReporter.ReportProgressAsync(new TaskProgress
        {
            CurrentPhase = "Connecting",
            StatusMessage = "Establishing connection to data source...",
            PercentComplete = 0
        });
        await Task.Delay(800, cancellationToken);

        // Phase 2: Fetching Schema
        await progressReporter.ReportProgressAsync(new TaskProgress
        {
            CurrentPhase = "Schema Analysis",
            StatusMessage = "Analyzing data schema...",
            PercentComplete = 5,
            CustomData = new Dictionary<string, object?>
            {
                ["tablesFound"] = 3,
                ["connectionStatus"] = "Connected"
            }
        });
        await Task.Delay(600, cancellationToken);

        // Phase 3: Syncing Records
        for (int i = 1; i <= recordCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Simulate occasional slower operations
            var delay = i % 10 == 0 ? 150 : 50;
            
            // Simulate occasional errors (for demonstration)
            if (i % 17 == 0)
            {
                errors++;
            }
            else
            {
                syncedRecords++;
            }

            var percent = 10 + (int)((double)i / recordCount * 85);

            await progressReporter.ReportProgressAsync(new TaskProgress
            {
                CurrentPhase = "Synchronizing",
                StatusMessage = $"Syncing record {i} of {recordCount}...",
                CurrentItem = $"Record_{i:D5}",
                PercentComplete = percent,
                TotalItems = recordCount,
                ProcessedItems = i,
                EstimatedSecondsRemaining = (recordCount - i) * 0.08,
                CustomData = new Dictionary<string, object?>
                {
                    ["syncedRecords"] = syncedRecords,
                    ["errors"] = errors,
                    ["throughput"] = $"{(syncedRecords / (i * 0.08)):F1} records/sec"
                }
            });

            await Task.Delay(delay, cancellationToken);
        }

        // Phase 4: Verification
        await progressReporter.ReportProgressAsync(new TaskProgress
        {
            CurrentPhase = "Verification",
            StatusMessage = "Verifying synchronized data...",
            PercentComplete = 98
        });
        await Task.Delay(400, cancellationToken);

        // Complete
        await progressReporter.ReportProgressAsync(new TaskProgress
        {
            CurrentPhase = "Complete",
            StatusMessage = "Synchronization finished",
            PercentComplete = 100,
            TotalItems = recordCount,
            ProcessedItems = recordCount
        });

        return TaskResult.Successful(
            $"Synchronized {syncedRecords} records with {errors} errors",
            new Dictionary<string, object?>
            {
                ["totalRecords"] = recordCount,
                ["syncedRecords"] = syncedRecords,
                ["errors"] = errors
            });
    }

    private static bool TryGetInt(Dictionary<string, object?> parameters, string key, out int value)
    {
        value = 0;

        if (!parameters.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        if (raw is int i)
        {
            value = i;
            return true;
        }

        if (raw is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number => element.TryGetInt32(out value),
                JsonValueKind.String => int.TryParse(element.GetString(), out value),
                _ => false
            };
        }

        return int.TryParse(raw.ToString(), out value);
    }
}
