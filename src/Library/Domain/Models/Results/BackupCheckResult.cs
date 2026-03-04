using System.Collections.Generic;
using Library.Domain.Abstractions;

namespace Library.Domain.Models.Results;

/// <summary>
/// Result of Anchor's backup chain integrity check.
/// Covers snapshot recency, encryption status, and size anomalies.
/// </summary>
public sealed class BackupCheckResult : ActionableResult
{
    /// <summary>Whether all expected sources had a current backup within the RPO window.</summary>
    public bool AllBackupsCurrent { get; set; }

    /// <summary>Whether encryption-at-rest was confirmed on backup targets.</summary>
    public bool EncryptionVerified { get; set; }

    /// <summary>Number of sources with a missing or failed recent backup.</summary>
    public int FailedSourceCount { get; set; }

    /// <summary>Individual flag narratives (one per failing source or anomaly).</summary>
    public List<string> Flags { get; set; } = [];
}
