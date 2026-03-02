using System;
using System.Collections.Generic;
using System.Linq;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Produces a high-level health summary across all control layers.
/// This is a composite collector that summarizes rather than executing a single command.
/// Output format: Layer|Status|Signal
/// </summary>
public class ControlMapCollector : ShellCollector<ControlMapMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        return @"
            echo ""Hardware|$(sensors 2>/dev/null | grep -q 'Package' && echo 'OK' || echo 'Degraded')|ThermalCheck"";
            echo ""Kernel|OK|$(uname -r)"";
            echo ""Security|$(sudo ss -tulpn 2>/dev/null | grep -c LISTEN) open ports|PortScan"";
            echo ""GPU|$(nvidia-smi --query-gpu=utilization.gpu --format=csv,noheader 2>/dev/null || echo 'N/A')|GpuCheck"";
            echo ""Automation|$(systemctl list-timers --no-legend 2>/dev/null | wc -l) timers|TimerCheck"";
        ";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"
$kernelVer = [System.Environment]::OSVersion.Version.ToString()
$fwProfiles = (Get-NetFirewallProfile | Where-Object {$_.Enabled -eq $true}).Count
$fwStatus = if ($fwProfiles -gt 0) {'OK'} else {'Degraded'}
$gpuCheck = if (Get-Command nvidia-smi -ErrorAction SilentlyContinue) {'OK'} else {'N/A'}
$taskCount = (Get-ScheduledTask | Where-Object {$_.State -ne 'Disabled'}).Count
Write-Output ""Hardware|OK|WMI""
Write-Output ""Kernel|OK|$kernelVer""
Write-Output ""Security|$fwStatus|FirewallCheck""
Write-Output ""GPU|$gpuCheck|GpuCheck""
Write-Output ""Automation|$taskCount tasks|TaskCheck""
";
    }

    protected override IEnumerable<ControlMapMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<ControlMapMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 3) continue;

            var status = parts[1].Trim();
            results.Add(new ControlMapMetric
            {
                Timestamp = collectedAt,
                Layer = parts[0].Trim(),
                Status = status.Contains("OK") || status.All(char.IsDigit) ? "OK" : status,
                Signal = parts[2].Trim(),
                ActionRequired = status.Contains("Degraded") ? "Yes" : "No"
            });
        }
        return results;
    }
}
