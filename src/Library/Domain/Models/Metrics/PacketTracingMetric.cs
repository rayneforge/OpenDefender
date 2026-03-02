using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: packet_tracing.csv — Timestamp,Interface,PacketsCaptured,Status</summary>
public class PacketTracingMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Interface { get; set; }
    public int PacketsCaptured { get; set; } // Changed from string to int
    public string Status { get; set; }
}
