using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: logging.csv — Timestamp,Component,Metric,Value,Status</summary>
public class LoggingMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Component { get; set; }
    public required string Metric { get; set; } // e.g. "ErrorRate", "Volume"
    public double Value { get; set; }  // Changed from string to double
    public required string Status { get; set; }
}
