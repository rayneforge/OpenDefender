using System.Collections.Generic;
using Library.Domain.Abstractions;

namespace Library.Domain.Models.Results;

/// <summary>
/// Result of Anchor's service stability check.
/// Covers failed services, restart counts, and scheduled timer health.
/// </summary>
public sealed class ServiceStabilityResult : ActionableResult
{
    /// <summary>Whether all monitored services were stable during the period.</summary>
    public bool AllServicesStable { get; set; }

    /// <summary>Number of services found in a failed or inactive state.</summary>
    public int FailedServiceCount { get; set; }

    /// <summary>Whether scheduled timers and automation jobs were at expected counts.</summary>
    public bool TimersHealthy { get; set; }

    /// <summary>Individual flag narratives for systemic service failures.</summary>
    public List<string> Flags { get; set; } = [];
}
