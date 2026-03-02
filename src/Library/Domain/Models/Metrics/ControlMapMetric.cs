using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: control_map.csv — Timestamp,Layer,Status,Signal,Action_Required</summary>
public class ControlMapMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Layer { get; set; }
    public string Status { get; set; }
    public string Signal { get; set; }
    public string ActionRequired { get; set; }
}
