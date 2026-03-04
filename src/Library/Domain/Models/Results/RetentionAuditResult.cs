using System.Collections.Generic;
using Library.Domain.Abstractions;

namespace Library.Domain.Models.Results;

/// <summary>
/// Result of Ledger's log retention audit.
/// Covers retention compliance per stream against defined policy targets.
/// </summary>
public sealed class RetentionAuditResult : ActionableResult
{
    /// <summary>Whether all log streams met their retention policy targets.</summary>
    public bool AllStreamsCompliant { get; set; }

    /// <summary>Streams identified as below their policy retention target.</summary>
    public List<string> NonCompliantStreams { get; set; } = [];

    /// <summary>Log sources identified as disproportionate noise consumers.</summary>
    public List<string> HighNoiseSources { get; set; } = [];
}
