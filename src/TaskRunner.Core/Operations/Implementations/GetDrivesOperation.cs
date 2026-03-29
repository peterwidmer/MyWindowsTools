using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TaskRunner.Core.Operations.Implementations;

public class GetDrivesOperation : ISyncOperation
{
    public string OperationType => "get-drives";
    public string DisplayName => "Get Logical Drives";

    public Task<OperationResult> ExecuteAsync(Dictionary<string, object?> parameters)
    {
        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => new
                {
                    Name = d.Name,
                    VolumeLabel = d.VolumeLabel,
                    DriveType = d.DriveType.ToString(),
                    TotalSize = d.TotalSize,
                    AvailableFreeSpace = d.AvailableFreeSpace,
                    DriveFormat = d.DriveFormat
                })
                .ToList();

            var data = new Dictionary<string, object?>
            {
                ["drives"] = drives
            };

            return Task.FromResult(OperationResult.Successful("Successfully retrieved drives.", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failed($"Failed to get drives: {ex.Message}"));
        }
    }
}
