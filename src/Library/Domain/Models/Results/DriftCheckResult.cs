using System.Collections.Generic;
using Library.Domain.Abstractions;

namespace Library.Domain.Models.Results;

/// <summary>
/// Result of Anchor's configuration drift check.
/// Covers divergences from desired state detected via automation tooling.
/// </summary>
public sealed class DriftCheckResult : ActionableResult
{
    /// <summary>Whether any configuration drift was detected.</summary>
    public bool DriftDetected { get; set; }

    /// <summary>Individual drift items: what changed, expected vs actual, and authorization status.</summary>
    public List<string> DriftItems { get; set; } = [];
}
