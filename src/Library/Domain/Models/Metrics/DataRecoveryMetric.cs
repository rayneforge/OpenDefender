using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: data_recovery.csv — Timestamp,Source,Status,Size</summary>
public class DataRecoveryMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Source { get; set; }
    public required string Status { get; set; }
    public long SizeBytes { get; set; } // Changed to long
}
