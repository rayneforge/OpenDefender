using System.Collections.Generic;
using Library.Domain.Abstractions;

namespace Library.Domain.Models.Results;

/// <summary>
/// Result of any agent's health brief compilation.
/// Shared across Shield, Anchor, Core, and Ledger briefs.
/// </summary>
public sealed class HealthBriefResult : ActionableResult
{
    /// <summary>The top risks or issues identified this period.</summary>
    public List<string> TopRisks { get; set; } = [];

    /// <summary>Notable changes observed during the reporting period.</summary>
    public List<string> Changes { get; set; } = [];

    /// <summary>Open items not yet resolved.</summary>
    public List<string> OpenItems { get; set; } = [];
}
