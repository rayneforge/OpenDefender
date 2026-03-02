using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects service status via systemctl (Linux) or Get-Service (Windows).
/// Output format: Service|Status|Timestamp
/// </summary>
public class ServiceCollector : ShellCollector<ServiceMetric>
{
    private readonly string[] _services;

    public ServiceCollector(params string[] services)
    {
        if (services.Length > 0)
        {
            _services = services;
        }
        else if (OperatingSystem.IsWindows())
        {
            _services = ["sshd", "docker", "W3SVC", "WinRM"];
        }
        else
        {
            _services = ["sshd", "docker", "nginx", "prometheus"];
        }
    }

    protected override string BuildLinuxCommand(DateTime? since)
    {
        var checks = string.Join("; ", Array.ConvertAll(_services, svc =>
            $"echo \"{svc}|$(systemctl is-active {svc} 2>/dev/null)|$(systemctl show -p ActiveEnterTimestamp {svc} 2>/dev/null | cut -d= -f2)\""));
        return checks;
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        var svcList = string.Join("','", _services);
        return @"$svcs = @('" + svcList + @"')
foreach ($name in $svcs) {
    $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
    if ($svc -and $svc.Status -eq 'Running') {
        try {
            $cim = Get-CimInstance Win32_Service -Filter ""Name='$name'"" -ErrorAction Stop
            if ($cim.ProcessId -gt 0) {
                $proc = Get-Process -Id $cim.ProcessId -ErrorAction Stop
                Write-Output ""$name|active|$($proc.StartTime.ToString('o'))""
            } else { Write-Output ""$name|active|"" }
        } catch { Write-Output ""$name|active|"" }
    } elseif ($svc) { Write-Output ""$name|$($svc.Status.ToString().ToLower())|"" }
    else { Write-Output ""$name|inactive|"" }
}
";
    }

    protected override IEnumerable<ServiceMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<ServiceMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 3) continue;

            var serviceName = parts[0].Trim();
            var status = parts[1].Trim();
            var timestampStr = parts[2].Trim();
            
            double uptime = 0;
            if (!string.IsNullOrEmpty(timestampStr) && DateTime.TryParse(timestampStr, out var startTime))
            {
                uptime = (collectedAt - startTime).TotalSeconds;
            }
            if (uptime < 0) uptime = 0;

            results.Add(new ServiceMetric
            {
                Timestamp = collectedAt,
                Service = serviceName,
                Status = status,
                UptimeSeconds = uptime
            });
        }
        return results;
    }
}
