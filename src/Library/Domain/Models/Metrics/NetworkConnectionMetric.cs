namespace Library.Domain.Models.Metrics;

/// <summary>A current TCP endpoint snapshot; no packet payload or historical traffic is stored.</summary>
public sealed class NetworkConnectionMetric
{
    public DateTime Timestamp { get; set; }
    public string Protocol { get; set; } = "TCP";
    public required string LocalAddress { get; set; }
    public int? LocalPort { get; set; }
    public required string RemoteAddress { get; set; }
    public int? RemotePort { get; set; }
    public required string State { get; set; }
}
