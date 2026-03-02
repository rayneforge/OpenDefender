using System;
using System.Collections.Generic;
using System.Globalization;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects CPU, memory, and load metrics via top/free/uptime (Linux) or CIM (Windows).
/// Output format: Metric|Value|Threshold
/// </summary>
public class ResourceCollector : ShellCollector<ResourceMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        return @"
            echo ""CPU_Usage|$(top -bn1 | grep 'Cpu(s)' | awk '{print $2 + $4}')|90"";
            echo ""Memory_Usage|$(free -m | awk '/Mem:/ {print $3}')|$(free -m | awk '/Mem:/ {print $2}')"";
            echo ""Load_Avg_1m|$(uptime | awk -F'load average:' '{print $2}' | awk -F',' '{print $1}' | xargs)|4.0"";
        ";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"
$cpu = (Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average).Average
Write-Output ""CPU_Usage|$cpu|90""
$os = Get-CimInstance Win32_OperatingSystem
$memUsedMB = [math]::Round(($os.TotalVisibleMemorySize - $os.FreePhysicalMemory) / 1024)
$memTotalMB = [math]::Round($os.TotalVisibleMemorySize / 1024)
Write-Output ""Memory_Usage|$memUsedMB|$memTotalMB""
try {
    $q = (Get-Counter '\System\Processor Queue Length' -ErrorAction Stop).CounterSamples[0].CookedValue
} catch { $q = 0 }
$cores = $env:NUMBER_OF_PROCESSORS
Write-Output ""Load_Avg_1m|$q|$cores""
";
    }

    protected override IEnumerable<ResourceMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<ResourceMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 3) continue;

            double.TryParse(parts[1].Trim(), out double val);
            double.TryParse(parts[2].Trim(), out double threshold);

            results.Add(new ResourceMetric
            {
                Timestamp = collectedAt,
                Metric = parts[0].Trim(),
                Value = val,
                Threshold = threshold
            });
        }
        return results;
    }
}
