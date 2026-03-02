using System;

namespace Library.Domain.Models.Metrics;

public class SecurityCheck
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string CheckType { get; set; } // e.g. "Firewall", "Network", "Auth"
    public required string Item { get; set; }      // e.g. "Status", "OpenPorts_Count"
    public required string Result { get; set; }    // e.g. "Active", "12" (legacy/descriptive)
    public double Value { get; set; }     // NEW: Numeric value for threshold checks (1.0 for Active, 12.0 for ports)
    public required string Severity { get; set; }
}
