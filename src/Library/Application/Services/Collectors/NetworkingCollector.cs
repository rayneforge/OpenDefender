using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Collects network interface metrics via ip (Linux) or Get-NetAdapterStatistics (Windows).
/// Output format: Interface|Metric|Value|Status
/// </summary>
public class NetworkingCollector : ShellCollector<NetworkingMetric>
{
    protected override string BuildLinuxCommand(DateTime? since)
    {
        return @"
            ip -s link show | awk '
                /^[0-9]+:/ { iface=$2; gsub(/:/,"""",iface) } 
                /RX:/ { getline; print iface ""|RX_Bytes|"" $1 ""|OK"" } 
                /TX:/ { getline; print iface ""|TX_Bytes|"" $1 ""|OK"" }'
        ";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        return @"
Get-NetAdapterStatistics -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Output ""$($_.Name)|RX_Bytes|$($_.ReceivedBytes)|OK""
    Write-Output ""$($_.Name)|TX_Bytes|$($_.SentBytes)|OK""
}
";
    }

    protected override IEnumerable<NetworkingMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<NetworkingMetric>();
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length < 4) continue;

            double.TryParse(parts[2].Trim(), out double val);

            results.Add(new NetworkingMetric
            {
                Timestamp = collectedAt,
                Interface = parts[0].Trim(),
                Metric = parts[1].Trim(), // RX_Bytes, TX_Bytes
                Value = val,
                Status = parts[3].Trim()
            });
        }
        return results;
    }
}
