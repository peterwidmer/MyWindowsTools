using System.Text.Json;

namespace TaskRunner.Core.Tasks.Implementations;

/// <summary>
/// Sample long-running task that simulates file processing with detailed progress.
/// </summary>
public class FileProcessingTask : IBackgroundTask
{
    public string TaskType => "file-processing";
    public string DisplayName => "File Processing";

    public async Task<TaskResult> ExecuteAsync(
        Dictionary<string, object?> parameters,
        IProgressReporter progressReporter,
        CancellationToken cancellationToken)
    {
        var fileCount = TryGetInt(parameters, "fileCount", out var count) ? Math.Max(1, count) : 10;
        var processedFiles = new List<string>();

        await progressReporter.ReportProgressAsync(new TaskProgress
        {
            CurrentPhase = "Initialization",
            StatusMessage = "Preparing file processing...",
            PercentComplete = 0,
            TotalItems = fileCount,
            ProcessedItems = 0
        });

        await Task.Delay(500, cancellationToken);

        for (int i = 1; i <= fileCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = $"document_{i:D3}.txt";
            processedFiles.Add(fileName);

            await progressReporter.ReportProgressAsync(new TaskProgress
            {
                CurrentPhase = "Processing Files",
                StatusMessage = $"Processing {fileName}...",
                CurrentItem = fileName,
                PercentComplete = (int)((double)i / fileCount * 100),
                TotalItems = fileCount,
                ProcessedItems = i,
                EstimatedSecondsRemaining = (fileCount - i) * 0.3,
                CustomData = new Dictionary<string, object?>
                {
                    ["currentFileName"] = fileName,
                    ["bytesProcessed"] = i * 1024
                }
            });

            await Task.Delay(300, cancellationToken);
        }

        await progressReporter.ReportProgressAsync(new TaskProgress
        {
            CurrentPhase = "Finalizing",
            StatusMessage = "Completing file processing...",
            PercentComplete = 100,
            TotalItems = fileCount,
            ProcessedItems = fileCount
        });

        return TaskResult.Successful(
            $"Successfully processed {fileCount} files",
            new Dictionary<string, object?>
            {
                ["processedFiles"] = processedFiles,
                ["totalBytesProcessed"] = fileCount * 1024
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
