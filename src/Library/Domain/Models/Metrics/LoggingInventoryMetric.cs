using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: logging_inventory.csv — Timestamp,Log_Source,Log_Type,Size,Status</summary>
public class LoggingInventoryMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string LogSource { get; set; }
    public string LogType { get; set; }
    public long SizeBytes { get; set; } // Changed from string Size to long SizeBytes
    public string Status { get; set; }
}
