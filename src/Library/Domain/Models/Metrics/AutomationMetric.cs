using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: automation.csv — Timestamp,Tool,Job,Result</summary>
public class AutomationMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Tool { get; set; }
    public required string Job { get; set; }
    public required string Result { get; set; }
}
