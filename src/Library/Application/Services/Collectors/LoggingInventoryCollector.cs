using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects per-file log inventory from /var/log (Linux) or Windows Event Logs.
/// Output format: LogSource|LogType|SizeBytes|Status
/// </summary>
public class LoggingInventoryCollector : ShellCollector<LoggingInventoryMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        // Normalize to pipe-delimited: LogSource|LogType|SizeBytes|Status
        return "find /var/log -maxdepth 2 -type f -name '*.log' -exec du -b {} + 2>/dev/null | sort -rn | head -20 | awk -F'\\t' '{print $2 \"|Text|\" $1 \"|OK\"}'";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"Get-ChildItem ""$env:SystemRoot\System32\winevt\Logs\*.evtx"" -ErrorAction SilentlyContinue | Sort-Object Length -Descending | Select-Object -First 20 | ForEach-Object { ""$($_.FullName)|EventLog|$($_.Length)|OK"" }";
    }

    protected override IEnumerable<LoggingInventoryMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<LoggingInventoryMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 4) continue;

            long.TryParse(parts[2].Trim(), out long sizeBytes);

            results.Add(new LoggingInventoryMetric
            {
                Timestamp = collectedAt,
                LogSource = parts[0].Trim(),
                LogType = parts[1].Trim(),
                SizeBytes = sizeBytes,
                Status = parts[3].Trim()
            });
        }
        return results;
    }
}
