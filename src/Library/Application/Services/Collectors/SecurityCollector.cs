using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects security signals: firewall, open ports, failed logins.
/// Linux: ufw/ss/journalctl. Windows: NetFirewall/NetTCPConnection/Security EventLog.
/// Output format: CheckType|Item|Result|Value|Severity
/// </summary>
public class SecurityCollector : ShellCollector<SecurityCheck>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        var sinceArg = since.HasValue
            ? $"--since '{since.Value:yyyy-MM-dd HH:mm:ss}'"
            : "--since '24 hours ago'";

        return $@"
            echo ""Firewall|Status|$(ufw status 2>/dev/null | grep 'Status: active' >/dev/null && echo 'Active' || echo 'Inactive')|$(ufw status 2>/dev/null | grep -c 'Status: active')|Medium"";
            echo ""Network|OpenPorts|Count|$(ss -tulpn 2>/dev/null | grep LISTEN | wc -l)|Low"";
            echo ""Auth|FailedLogins|Count|$(journalctl _COMM=sshd {sinceArg} 2>/dev/null | grep -c 'Failed password')|High"";
        ";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        var sinceExpr = since.HasValue
            ? $"(Get-Date '{since.Value:yyyy-MM-dd HH:mm:ss}')"
            : "(Get-Date).AddHours(-24)";

        return $@"
$fwEnabled = (Get-NetFirewallProfile | Where-Object {{$_.Enabled -eq $true}}).Count
$fwResult = if ($fwEnabled -gt 0) {{'Active'}} else {{'Inactive'}}
Write-Output ""Firewall|Status|$fwResult|$fwEnabled|Medium""
$ports = (Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Measure-Object).Count
Write-Output ""Network|OpenPorts|Count|$ports|Low""
try {{
    $failed = (Get-WinEvent -FilterHashtable @{{LogName='Security';Id=4625;StartTime={sinceExpr}}} -ErrorAction Stop).Count
}} catch {{ $failed = 0 }}
Write-Output ""Auth|FailedLogins|Count|$failed|High""
";
    }

    protected override IEnumerable<SecurityCheck> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<SecurityCheck>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 5) continue;

            double.TryParse(parts[3].Trim(), out double val);

            results.Add(new SecurityCheck
            {
                Timestamp = collectedAt,
                CheckType = parts[0].Trim(),
                Item = parts[1].Trim(),
                Result = parts[2].Trim(),
                Value = val,
                Severity = parts[4].Trim()
            });
        }
        return results;
    }
}
