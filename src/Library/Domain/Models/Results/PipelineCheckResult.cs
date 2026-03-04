using System.Collections.Generic;
using Library.Domain.Abstractions;

namespace Library.Domain.Models.Results;

/// <summary>
/// Result of Ledger's daily logging pipeline check.
/// Covers journal health, log source presence, and ingestion backlog.
/// </summary>
public sealed class PipelineCheckResult : ActionableResult
{
    /// <summary>Whether the pipeline was confirmed healthy with no drops or backlog.</summary>
    public bool PipelineHealthy { get; set; }

    /// <summary>Number of expected log sources that were absent or silent.</summary>
    public int MissingSourceCount { get; set; }

    /// <summary>Whether an ingestion backlog or dropped events were detected.</summary>
    public bool BacklogDetected { get; set; }

    /// <summary>Individual flag narratives (one per missing source or pipeline fault).</summary>
    public List<string> Flags { get; set; } = [];
}
