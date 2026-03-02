using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: automation.csv — Timestamp,Tool,Job,Result</summary>
public class AutomationMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Tool { get; set; }
    public string Job { get; set; }
    public string Result { get; set; }
}
