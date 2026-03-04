using System.Collections.Generic;
using Library.Domain.Abstractions;

namespace Library.Domain.Models.Results;

/// <summary>
/// Result of Shield's policy drift check.
/// Covers access-control profiles, IAM changes, and new network listeners.
/// </summary>
public sealed class PolicyDriftResult : ActionableResult
{
    /// <summary>Whether any policy drift was detected against the baseline.</summary>
    public bool DriftDetected { get; set; }

    /// <summary>Individual drift findings (profile name, rule change, or new listener).</summary>
    public List<string> DriftFindings { get; set; } = [];
}
