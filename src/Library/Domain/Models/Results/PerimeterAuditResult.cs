using System.Collections.Generic;
using Library.Domain.Abstractions;

namespace Library.Domain.Models.Results;

/// <summary>
/// Result of Shield's daily perimeter audit.
/// Covers firewall posture, auth failures, interface state, and packet anomalies.
/// </summary>
public sealed class PerimeterAuditResult : ActionableResult
{
    /// <summary>Whether the firewall was confirmed active.</summary>
    public bool FirewallActive { get; set; }

    /// <summary>Number of open ports observed.</summary>
    public int OpenPortCount { get; set; }

    /// <summary>Number of authentication failures detected in the period.</summary>
    public int AuthFailureCount { get; set; }

    /// <summary>Whether any network interface was found in a degraded or missing state.</summary>
    public bool InterfaceIssues { get; set; }

    /// <summary>Whether any packet capture reported anomaly status.</summary>
    public bool PacketAnomalies { get; set; }

    /// <summary>Individual flag narratives produced by the agent (one per finding).</summary>
    public List<string> Flags { get; set; } = [];
}
