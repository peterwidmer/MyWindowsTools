using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TaskRunner.Core.Tasks.Implementations;

public class DirectorySizeTask : IBackgroundTask
{
    public string TaskType => "directory-size";
    public string DisplayName => "Directory Size Analyzer";

    public async Task<TaskResult> ExecuteAsync(
        Dictionary<string, object?> parameters,
        IProgressReporter progressReporter,
        CancellationToken cancellationToken)
    {
        var targetPath = GetString(parameters, "targetPath") ?? @"C:\";

        if (!Directory.Exists(targetPath))
        {
            return TaskResult.Failed($"Directory does not exist: {targetPath}");
        }

        var rootInfo = new DirectoryInfo(targetPath);
        var statsMap = new ConcurrentDictionary<string, FolderStats>();

        // Pre-populate immediate children
        try
        {
            var topDirs = rootInfo.GetDirectories();
            foreach (var defaultDir in topDirs)
            {
                statsMap.TryAdd(defaultDir.Name, new FolderStats
                {
                    Name = defaultDir.Name,
                    Path = defaultDir.FullName,
                    IsDirectory = true
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore access errors on root
        }

        var rootFiles = new FolderStats
        {
            Name = "<Files in root>",
            Path = targetPath,
            IsDirectory = false
        };

        long totalSizeBytes = 0;
        int totalFiles = 0;
        int totalFolders = 0;
        string currentScannedPath = targetPath;
        
        var nextReportTime = DateTime.UtcNow.AddMilliseconds(500);

        // A helper to report progress
        async Task ReportProgressAsync(string currentPath)
        {
            var now = DateTime.UtcNow;
            if (now < nextReportTime) return;
            nextReportTime = now.AddMilliseconds(500);

            var itemsList = statsMap.Values.ToList();
            if (rootFiles.SizeBytes > 0 || rootFiles.FileCount > 0)
            {
                itemsList.Add(rootFiles);
            }

            // Sort by size descending
            itemsList = itemsList.OrderByDescending(x => x.SizeBytes).ToList();

            var currentStats = new Dictionary<string, object?>
            {
                ["totals"] = new
                {
                    sizeBytes = Interlocked.Read(ref totalSizeBytes),
                    files = totalFiles,
                    folders = totalFolders
                },
                ["items"] = itemsList
            };

            await progressReporter.ReportProgressAsync(new TaskProgress
            {
                CurrentPhase = "Scanning",
                StatusMessage = $"Scanning: {totalFiles:N0} files found",
                CurrentItem = currentPath,
                TotalItems = null,
                ProcessedItems = totalFiles,
                CustomData = currentStats
            });
        }

        // Process files in root
        try
        {
            foreach (var file in rootInfo.GetFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();
                rootFiles.SizeBytes += file.Length;
                rootFiles.FileCount++;
                Interlocked.Add(ref totalSizeBytes, file.Length);
                Interlocked.Increment(ref totalFiles);
            }
        }
        catch (Exception) { /* Ignored */ }

        await ReportProgressAsync(targetPath);

        // Process directories in parallel
        var directoriesToProcess = statsMap.Values.ToList();
        
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };

        var enumOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false
        };

        await Parallel.ForEachAsync(directoriesToProcess, options, async (folderStats, ct) =>
        {
            try
            {
                // We do a manual stack-based or queue-based traversal to periodically yield and report progress,
                // or just use EnumerateFileSystemInfos and report periodically.
                var dirInfo = new DirectoryInfo(folderStats.Path);
                
                foreach (var fsi in dirInfo.EnumerateFileSystemInfos("*", enumOptions))
                {
                    ct.ThrowIfCancellationRequested();
                    
                    if (fsi is FileInfo fi)
                    {
                        folderStats.SizeBytes += fi.Length;
                        folderStats.FileCount++;
                        Interlocked.Add(ref totalSizeBytes, fi.Length);
                        Interlocked.Increment(ref totalFiles);
                    }
                    else if (fsi is DirectoryInfo)
                    {
                        folderStats.FolderCount++;
                        Interlocked.Increment(ref totalFolders);
                    }

                    // Only one parallel task reports progress to keep it simple, or they all try and the time-check throttles it.
                    // To avoid locks, we just let them call it. The method handles time throttling roughly.
                    currentScannedPath = fsi.FullName;
                    
                    // Throttle checking the time to avoid DateTime.UtcNow overhead on every single file
                    if (folderStats.FileCount % 1000 == 0)
                    {
                        await ReportProgressAsync(currentScannedPath);
                    }
                }
            }
            catch (Exception)
            {
                // Ignore inaccessible top-level folders
            }
        });

        // Final Report
        var finalList = statsMap.Values.ToList();
        if (rootFiles.SizeBytes > 0 || rootFiles.FileCount > 0)
        {
            finalList.Add(rootFiles);
        }
        finalList = finalList.OrderByDescending(x => x.SizeBytes).ToList();

        var resultData = new Dictionary<string, object?>
        {
            ["totals"] = new
            {
                sizeBytes = totalSizeBytes,
                files = totalFiles,
                folders = totalFolders
            },
            ["items"] = finalList
        };

        await progressReporter.ReportProgressAsync(new TaskProgress
        {
            CurrentPhase = "Completed",
            StatusMessage = $"Scan completed: {totalFiles:N0} files",
            CurrentItem = "",
            PercentComplete = 100,
            ProcessedItems = totalFiles,
            CustomData = resultData
        });

        return TaskResult.Successful("Directory scan completed.", resultData);
    }

    private static string? GetString(Dictionary<string, object?> parameters, string key)
    {
        if (!parameters.TryGetValue(key, out var raw) || raw is null) return null;
        if (raw is JsonElement el && el.ValueKind == JsonValueKind.String) return el.GetString();
        return raw.ToString();
    }
}

public class FolderStats
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int FileCount { get; set; }
    public int FolderCount { get; set; }
    public bool IsDirectory { get; set; }
}
