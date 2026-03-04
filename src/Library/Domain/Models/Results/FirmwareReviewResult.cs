using System.Collections.Generic;
using Library.Domain.Abstractions;

namespace Library.Domain.Models.Results;

/// <summary>
/// Result of Core's firmware and kernel review.
/// Covers kernel version, security parameters, boot performance, and disk health.
/// </summary>
public sealed class FirmwareReviewResult : ActionableResult
{
    /// <summary>The current running kernel version string.</summary>
    public string? KernelVersion { get; set; }

    /// <summary>Whether kptr_restrict is set to a non-zero (compliant) value.</summary>
    public bool KptrRestrictCompliant { get; set; }

    /// <summary>Whether any pending firmware updates were identified.</summary>
    public bool PendingFirmwareUpdates { get; set; }

    /// <summary>Individual flag narratives (one per finding at S2 or above).</summary>
    public List<string> Flags { get; set; } = [];
}
