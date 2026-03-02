using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: networking.csv — Timestamp,Interface,Metric,Value,Status</summary>
public class NetworkingMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Interface { get; set; }
    public string Metric { get; set; } // e.g. "RX_Bytes", "TX_Packets"
    public double Value { get; set; }  // Changed from string to double
    public string Status { get; set; }
}
