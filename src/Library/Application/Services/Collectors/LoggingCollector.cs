using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects journald disk usage (Linux) or Windows Event Log status.
/// Output format: Component|Metric|Value|Status
/// </summary>
public class LoggingCollector : ShellCollector<LoggingMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        return @"
            echo ""JournalD|DiskUsageBytes|$(du -sb /var/log/journal 2>/dev/null | awk '{print $1}' || echo 0)|OK"";
            echo ""JournalD|Boots|$(journalctl --list-boots 2>/dev/null | wc -l)|OK"";
        ";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"
$logDir = ""$env:SystemRoot\System32\winevt\Logs""
$size = (Get-ChildItem $logDir -File -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
Write-Output ""EventLog|DiskUsageBytes|$size|OK""
$logCount = (Get-WinEvent -ListLog * -ErrorAction SilentlyContinue).Count
Write-Output ""EventLog|LogCount|$logCount|OK""
";
    }

    protected override IEnumerable<LoggingMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<LoggingMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 4) continue;

            double.TryParse(parts[2].Trim(), out double val);

            results.Add(new LoggingMetric
            {
                Timestamp = collectedAt,
                Component = parts[0].Trim(),
                Metric = parts[1].Trim(),
                Value = val,
                Status = parts[3].Trim()
            });
        }
        return results;
    }
}
