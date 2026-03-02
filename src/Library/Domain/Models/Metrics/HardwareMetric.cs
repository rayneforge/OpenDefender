using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: hardware_firmware.csv — Timestamp,Device,Attribute,Value,Status</summary>
public class HardwareMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Device { get; set; }
    public required string Attribute { get; set; }
    public double Value { get; set; } // Changed to double for threshold checks
    public required string Status { get; set; }
}
