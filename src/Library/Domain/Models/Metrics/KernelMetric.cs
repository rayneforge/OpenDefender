using System;

namespace Library.Domain.Models.Metrics;

public class KernelMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Category { get; set; }
    public string Metric { get; set; }
    public string Value { get; set; }
    public string Alert { get; set; }
}
