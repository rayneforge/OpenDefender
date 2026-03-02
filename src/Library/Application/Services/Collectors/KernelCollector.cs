using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects kernel and OS metrics via uname/sysctl (Linux) or CIM/Registry (Windows).
/// Output format: Category|Metric|Value
/// </summary>
public class KernelCollector : ShellCollector<KernelMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        return @"
            echo ""Kernel|Version|$(uname -r)"";
            echo ""Security|KptrRestrict|$(sysctl -n kernel.kptr_restrict)"";
            echo ""Boot|TotalTime|$(systemd-analyze 2>/dev/null | awk '/Startup finished/ {print $4}')"";
        ";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"
$ver = [System.Environment]::OSVersion.Version.ToString()
Write-Output ""Kernel|Version|$ver""
try {
    $uac = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name EnableLUA -ErrorAction Stop).EnableLUA
    Write-Output ""Security|UAC|$(if ($uac -eq 1) {'Enabled'} else {'Disabled'})""
} catch { Write-Output ""Security|UAC|Unknown"" }
try {
    $boot = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime
    $uptimeSec = [math]::Round(((Get-Date) - $boot).TotalSeconds)
    Write-Output ""Boot|TotalTime|${uptimeSec}s""
} catch { Write-Output ""Boot|TotalTime|Unknown"" }
";
    }

    protected override IEnumerable<KernelMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<KernelMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 3) continue;

            results.Add(new KernelMetric
            {
                Timestamp = collectedAt,
                Category = parts[0].Trim(),
                Metric = parts[1].Trim(),
                Value = parts[2].Trim(),
                Alert = "None"
            });
        }
        return results;
    }
}
