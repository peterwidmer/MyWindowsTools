using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskRunner.Core.Tasks;
using TaskRunner.Core.Tasks.Implementations;

namespace TaskRunner.Core.Tasks.Implementations;

public class FindFilesTask : IBackgroundTask
{
    public string TaskType => "find-files";
    public string DisplayName => "Find Files";

    public async Task<TaskResult> ExecuteAsync(
        Dictionary<string, object?> parameters,
        IProgressReporter progressReporter,
        CancellationToken cancellationToken)
    {
        var targetPath = GetString(parameters, "targetPath") ?? @"C:\";
        var filter = GetString(parameters, "filter");
        if (string.IsNullOrWhiteSpace(filter)) filter = "*.*";

        if (!Directory.Exists(targetPath))
        {
            return TaskResult.Failed($"Directory does not exist: {targetPath}");
        }

        var foundFiles = new ConcurrentBag<FileInfoData>();
        long totalMatchedFiles = 0;
        var nextReportTime = DateTime.UtcNow.AddMilliseconds(500);

        async Task ReportProgressAsync(bool final = false)
        {
            var now = DateTime.UtcNow;
            if (!final && now < nextReportTime) return;
            nextReportTime = now.AddMilliseconds(500);

            var itemsList = foundFiles.ToList();
            
            await progressReporter.ReportProgressAsync(new TaskProgress
            {
                CurrentPhase = final ? "Completed" : "Searching",
                StatusMessage = $"Found: {Interlocked.Read(ref totalMatchedFiles):N0} matches.",
                ProcessedItems = (int)Math.Min(Interlocked.Read(ref totalMatchedFiles), int.MaxValue),
                PercentComplete = final ? 100 : null,
                CustomData = new Dictionary<string, object?>
                {
                    ["items"] = itemsList
                }
            });
        }

        await Task.Run(async () =>
        {
            var options = new EnumerationOptions { IgnoreInaccessible = true };
            var queue = new Queue<string>();
            queue.Enqueue(targetPath);
            int directoriesChecked = 0;

            while (queue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var currentDir = queue.Dequeue();

                try
                {
                    foreach (var d in Directory.EnumerateDirectories(currentDir, "*", options))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        queue.Enqueue(d);
                    }
                }
                catch { }

                try
                {
                    var matchedInDirectory = 0;

                    foreach (var f in Directory.EnumerateFiles(currentDir, filter, options))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var fi = new FileInfo(f);
                        foundFiles.Add(new FileInfoData
                        {
                            Name = fi.Name,
                            Path = fi.FullName,
                            Directory = fi.DirectoryName ?? string.Empty,
                            SizeBytes = fi.Length,
                            LastModified = fi.LastWriteTimeUtc
                        });

                        matchedInDirectory++;
                        Interlocked.Increment(ref totalMatchedFiles);

                        if (matchedInDirectory % 100 == 0)
                        {
                            await ReportProgressAsync();
                        }
                    }

                    directoriesChecked++;
                    if (directoriesChecked % 25 == 0 || matchedInDirectory > 0)
                    {
                        await ReportProgressAsync();
                    }
                }
                catch { }
            }
        }, cancellationToken);

        await ReportProgressAsync(final: true);

        return TaskResult.Successful($"Search completed.", new Dictionary<string, object?>
        {
            ["items"] = foundFiles.ToList(),
            ["totalMatched"] = Interlocked.Read(ref totalMatchedFiles)
        });
    }

    private static string? GetString(Dictionary<string, object?> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var raw) || raw is null) return null;
        if (raw is JsonElement el && el.ValueKind == JsonValueKind.String) return el.GetString();
        return raw.ToString();
    }
}

public class FileInfoData
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Directory { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
}
