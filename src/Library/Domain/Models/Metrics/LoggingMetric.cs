using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: logging.csv — Timestamp,Component,Metric,Value,Status</summary>
public class LoggingMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Component { get; set; }
    public string Metric { get; set; } // e.g. "ErrorRate", "Volume"
    public double Value { get; set; }  // Changed from string to double
    public string Status { get; set; }
}
