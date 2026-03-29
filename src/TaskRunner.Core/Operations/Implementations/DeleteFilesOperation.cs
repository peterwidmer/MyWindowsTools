using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskRunner.Core.Operations.Implementations;

public class DeleteFilesOperation : ISyncOperation
{
    public string OperationType => "delete-files";
    public string DisplayName => "Delete Files";

    public Task<OperationResult> ExecuteAsync(Dictionary<string, object?> parameters)
    {
        var pathsObj = parameters.GetValueOrDefault("paths");
        var paths = new List<string>();

        if (pathsObj is JsonElement el && el.ValueKind == JsonValueKind.Array)
        {
            paths = el.EnumerateArray()
                      .Select(x => x.GetString())
                      .Where(x => !string.IsNullOrEmpty(x))
                      .Cast<string>()
                      .ToList();
        }
        else if (pathsObj is IEnumerable<object> list)
        {
            paths = list.Select(x => x?.ToString())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Cast<string>()
                        .ToList();
        }
        else if (pathsObj is string s)
        {
            paths.Add(s);
        }

        var deleted = new List<string>();
        var failed = new List<object>();

        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deleted.Add(path);
                }
                else
                {
                    failed.Add(new { path = path, error = "File not found" });
                }
            }
            catch (Exception ex)
            {
                failed.Add(new { path = path, error = ex.Message });
            }
        }

        var data = new Dictionary<string, object?>
        {
            ["deleted"] = deleted,
            ["failed"] = failed
        };

        if (failed.Count > 0)
        {
            return Task.FromResult(OperationResult.Successful($"Deleted {deleted.Count}, failed {failed.Count}", data)); 
        }

        return Task.FromResult(OperationResult.Successful($"Successfully deleted {deleted.Count} files.", data));
    }
}
