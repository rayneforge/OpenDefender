namespace Library.Domain.Abstractions;

/// <summary>
/// Severity level of an agent action result.
/// </summary>
public enum ResultLevel
{
    /// <summary>Normal, expected outcome. No action required.</summary>
    Info,

    /// <summary>Unexpected or degraded state. System still operational. Monitor closely.</summary>
    Warn,

    /// <summary>An operation failed or a component is unhealthy. Action recommended.</summary>
    Error,

    /// <summary>A critical component is unusable or data loss is at risk. Immediate action required.</summary>
    Critical
}

/// <summary>
/// Base class for all typed agent action results.
/// Every task produces at minimum a <see cref="Level"/> and a <see cref="Detail"/> narrative.
/// </summary>
public abstract class ActionableResult
{
    /// <summary>The inferred severity of the outcome.</summary>
    public ResultLevel Level { get; set; }

    /// <summary>The full narrative produced by the agent.</summary>
    public string Detail { get; set; } = string.Empty;
}
