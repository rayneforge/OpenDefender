using System;

namespace Library.Domain.Models.Metrics;

/// <summary>Matches: services_apps.csv — Timestamp,Service,Status,Uptime</summary>
public class ServiceMetric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Service { get; set; }
    public required string Status { get; set; }
    public double UptimeSeconds { get; set; } // Changed from string Uptime to double UptimeSeconds
}
