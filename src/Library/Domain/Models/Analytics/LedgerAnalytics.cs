using System;

namespace Library.Domain.Models.Analytics;

public class LedgerAnalytics
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string LogSource { get; set; } // The component name
    public required string LogType { get; set; } // Auth, Perf, App, System
    public long CurrentSizeBytes { get; set; }
    public long GrowthBytes { get; set; } // Delta size
    public double GrowthRateBytesPerHour { get; set; }
    public bool IsRetentionCompliant { get; set; } // Based on policy (180d/30d)
    public double RetentionDays { get; set; } // Estimated or Actual
    public bool GapDetected { get; set; } // If source is down/missing
    public double ShippingBacklog { get; set; } // Count or queue size
    public bool IsBacklogBreach { get; set; }
}
