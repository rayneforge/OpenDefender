using System;

namespace Library.Domain.Models.Analytics;

public class ReliabilityAnalytics
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Scope { get; set; } // Service, Logging, Inventory
    public string Entity { get; set; } // Service Name or Log Source
    public string StatusChange { get; set; } // "Active -> Failed", "None"
    public bool IsDegraded { get; set; } // e.g. Uptime reset, service down
    public bool GapDetected { get; set; } // e.g. Log source missing
}
