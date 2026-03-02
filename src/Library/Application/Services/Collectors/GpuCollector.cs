using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects GPU metrics via nvidia-smi (NVIDIA) on both Linux and Windows.
/// Output format: Vendor|Device|GpuUtil|MemUtil|Temp
/// </summary>
public class GpuCollector : ShellCollector<GpuMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        // output format: Vendor|Name|GpuUtil|MemUtil|Temp
        return @"
            if command -v nvidia-smi &>/dev/null; then
                nvidia-smi --query-gpu=name,utilization.gpu,utilization.memory,temperature.gpu --format=csv,noheader,nounits 2>/dev/null | while IFS=',' read -r name gpu mem temp; do
                    echo ""NVIDIA|$name|$gpu|$mem|$temp""
                done
            fi
        ";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"
if (Get-Command nvidia-smi -ErrorAction SilentlyContinue) {
    nvidia-smi --query-gpu=name,utilization.gpu,utilization.memory,temperature.gpu --format=csv,noheader,nounits 2>$null | ForEach-Object {
        $parts = $_ -split ','
        if ($parts.Count -ge 4) { ""NVIDIA|$($parts[0].Trim())|$($parts[1].Trim())|$($parts[2].Trim())|$($parts[3].Trim())"" }
    }
}
";
    }

    protected override IEnumerable<GpuMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<GpuMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 5) continue;

            double.TryParse(parts[2].Trim(), out double gpu);
            double.TryParse(parts[3].Trim(), out double mem);
            double.TryParse(parts[4].Trim(), out double temp);

            results.Add(new GpuMetric
            {
                Timestamp = collectedAt,
                Vendor = parts[0].Trim(),
                Device = parts[1].Trim(),
                GpuUtil = gpu,
                MemUtil = mem,
                Temp = temp
            });
        }
        return results;
    }
}
