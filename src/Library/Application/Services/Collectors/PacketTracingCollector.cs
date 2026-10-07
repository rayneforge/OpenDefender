using System;
using System.Collections.Generic;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Captures a brief packet sample via tcpdump (Linux) or counts active connections (Windows).
/// </summary>
public class PacketTracingCollector : ShellCollector<PacketTracingMetric>
{
    private readonly int _captureSeconds;

    public PacketTracingCollector(int captureSeconds = 1)
    {
        _captureSeconds = captureSeconds;
    }

    protected override string BuildLinuxCommand(DateTime? since)
    {
        return $"timeout {_captureSeconds}s tcpdump -i any -c 20 -q 2>/dev/null | wc -l";
    }

    protected override string BuildWindowsCommand(DateTime? since)
    {
        // No direct packet capture equivalent without admin + pktmon.
        // Count established TCP connections as a network activity proxy.
        return @"(Get-NetTCPConnection -State Established -ErrorAction SilentlyContinue | Measure-Object).Count";
    }

    protected override IEnumerable<PacketTracingMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var results = new List<PacketTracingMetric>();
        foreach (var line in lines)
        {
            if (int.TryParse(line.Trim(), out int count))
            {
                results.Add(new PacketTracingMetric
                {
                    Timestamp = collectedAt,
                    Interface = "any",
                    PacketsCaptured = count,
                    Status = count > 0 ? "Active" : "Idle"
                });
            }
        }
        return results;
    }
}
