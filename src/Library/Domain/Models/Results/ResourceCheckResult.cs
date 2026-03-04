using System.Collections.Generic;
using Library.Domain.Abstractions;

namespace Library.Domain.Models.Results;

/// <summary>
/// Result of Core's daily resource utilization check.
/// Covers CPU, memory, disk, thermal sensors, and GPU state.
/// </summary>
public sealed class ResourceCheckResult : ActionableResult
{
    /// <summary>Whether all resources were within the defined 30% headroom threshold.</summary>
    public bool WithinBaseline { get; set; }

    /// <summary>Observed CPU utilization percentage (0–100).</summary>
    public double? CpuPercent { get; set; }

    /// <summary>Observed memory utilization percentage (0–100).</summary>
    public double? MemPercent { get; set; }

    /// <summary>Observed disk utilization percentage (0–100).</summary>
    public double? DiskPercent { get; set; }

    /// <summary>Whether any thermal sensor was above warning threshold.</summary>
    public bool ThermalAlert { get; set; }

    /// <summary>Individual flag narratives (one per threshold breach or anomaly).</summary>
    public List<string> Flags { get; set; } = [];
}
