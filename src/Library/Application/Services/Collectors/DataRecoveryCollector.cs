using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects disk mount point usage via df (Linux) or Get-PSDrive (Windows).
/// Output format: Source|Status|SizeBytes
/// </summary>
public class DataRecoveryCollector : ShellCollector<DataRecoveryMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        // Normalize to pipe-delimited: Source|Status|UsedBytes
        return @"df -B1 --output=target,used -x tmpfs -x devtmpfs 2>/dev/null | tail -n +2 | grep -v '/boot' | awk '{print $1 ""|Mounted|"" $2}'";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"Get-PSDrive -PSProvider FileSystem | Where-Object {$_.Used -gt 0} | ForEach-Object { ""$($_.Root)|Mounted|$($_.Used)"" }";
    }

    protected override IEnumerable<DataRecoveryMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<DataRecoveryMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 3) continue;

            long.TryParse(parts[2].Trim(), out long usedBytes);

            results.Add(new DataRecoveryMetric
            {
                Timestamp = collectedAt,
                Source = parts[0].Trim(),
                Status = parts[1].Trim(),
                SizeBytes = usedBytes
            });
        }
        return results;
    }
}
