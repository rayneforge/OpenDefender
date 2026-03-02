using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: networking.csv — Timestamp,Interface,Metric,Value,Status</summary>
public class NetworkingMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Interface { get; set; }
    public required string Metric { get; set; } // e.g. "RX_Bytes", "TX_Packets"
    public double Value { get; set; }  // Changed from string to double
    public required string Status { get; set; }
}
