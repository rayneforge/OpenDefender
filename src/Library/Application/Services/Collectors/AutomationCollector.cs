using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects systemd timer (Linux) or scheduled task (Windows) status for automation monitoring.
/// Output format: Tool|Job|Result
/// </summary>
public class AutomationCollector : ShellCollector<AutomationMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        // Normalize to pipe-delimited: Tool|Job|Result
        return @"systemctl list-timers --no-legend --no-pager 2>/dev/null | head -20 | awk '{print ""SystemdTimer|"" $(NF-1) ""|Activates: "" $NF}'";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"Get-ScheduledTask | Where-Object {$_.State -ne 'Disabled'} | Select-Object -First 20 | ForEach-Object { ""ScheduledTask|$($_.TaskName)|$($_.State)"" }";
    }

    protected override IEnumerable<AutomationMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<AutomationMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 3) continue;

            results.Add(new AutomationMetric
            {
                Timestamp = collectedAt,
                Tool = parts[0].Trim(),
                Job = parts[1].Trim(),
                Result = parts[2].Trim()
            });
        }
        return results;
    }
}
