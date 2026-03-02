using System;

namespace Library.Domain.Models.Analytics;

public class ResourceAnalytics
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Metric { get; set; } // CPU, Memory, Disk path
    public double CurrentValue { get; set; }
    public double Delta { get; set; } // Change since last run
    public double Rate { get; set; } // Rate of change per second/unit
    public double RateDelta { get; set; } // Acceleration (change in rate)
    public bool IsBreach { get; set; }
    public required string Severity { get; set; } // Low, Medium, High, Critical
}
