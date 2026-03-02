using System;

namespace Library.Domain.Models.Analytics;

public class SecurityAnalytics
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string CheckType { get; set; } // Firewall, Auth, Network
    public int NewIssuesCount { get; set; } // e.g. New open ports since last check
    public bool IsBreach { get; set; } // e.g. Firewall inactive
    public required string Severity { get; set; }
}
