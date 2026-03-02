using System;

namespace Library.Domain.Models.Metrics;

public class KernelMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Category { get; set; }
    public required string Metric { get; set; }
    public required string Value { get; set; }
    public required string Alert { get; set; }
}
