using System;

namespace Library.Domain.Models.Metrics;

public class ResourceMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Metric { get; set; } // CPU, Memory
    public double Value { get; set; }  // Changed from string to double (percentage or MB)
    public double Threshold { get; set; } // Changed from string to double
}
