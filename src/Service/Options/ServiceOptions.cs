namespace Library.Domain.Models.State;

public class ServiceOptions
{
    public const string SectionName = "Service";

    /// <summary>
    /// Preferred communication mode: "Stdio" (MCP pattern) or "Http" (Rest API)
    /// </summary>
    public string TransportType { get; set; } = "Http";

    /// <summary>
    /// Frequency of background diagnostic collection (e.g. "00:05:00" for 5 minutes)
    /// </summary>
    public TimeSpan CollectionFrequency { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Connection string for the SQLite diagnostic database
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=diagnostic_reports.db";

    /// <summary>
    /// Historical window for the orchestrator (defaults to 24 hours)
    /// </summary>
    public int LookbackHours { get; set; } = 24;

    /// <summary>
    /// Number of minutes to retain data before the retention service purges it.
    /// </summary>
    public int RetentionMinutes { get; set; } = 120;

    /// <summary>
    /// How often the retention service runs (e.g. "00:15:00" for every 15 minutes).
    /// </summary>
    public TimeSpan RetentionFrequency { get; set; } = TimeSpan.FromMinutes(30);
}
