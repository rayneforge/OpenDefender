using System.ComponentModel;
using ModelContextProtocol.Server;
using Library.Domain.Models.Queries;
using Library.Infrastructure.Database;
using Library.Infrastructure.Query;
using System.Text.Json;

namespace Service.Mcp;

/// <summary>
/// Ledger domain — log retention, telemetry shipping, coverage, pipeline health.
/// Raw: LoggingMetrics, LoggingInventoryMetrics.
/// Derived: LedgerAnalytics.
/// </summary>
[McpServerToolType]
[McpServerPromptType]
public static class LoggingMcp
{
    // ── Raw metrics ─────────────────────────────────────────────────────

    [McpServerTool(Name = "query_logging_metrics", Title = "Query Logging Metrics")]
    [Description("Query raw logging pipeline metrics. Properties: Id (int), Timestamp (DateTime), Component (string), Metric (string), Value (double), Status (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryLoggingMetrics(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.LoggingMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "query_logging_inventory", Title = "Query Logging Inventory")]
    [Description("Query raw log source inventory. Properties: Id (int), Timestamp (DateTime), LogSource (string), LogType (string), SizeBytes (long), Status (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryLoggingInventory(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.LoggingInventoryMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    // ── Derived analytics ───────────────────────────────────────────────

    [McpServerTool(Name = "query_ledger_analytics", Title = "Query Ledger Analytics")]
    [Description("Query derived ledger analytics. Properties: Id (int), Timestamp (DateTime), LogSource (string), LogType (string), CurrentSizeBytes (long), GrowthBytes (long), GrowthRateBytesPerHour (double), IsRetentionCompliant (bool), RetentionDays (double), GapDetected (bool), ShippingBacklog (double), IsBacklogBreach (bool). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryLedgerAnalytics(QueryRequest request)
    {
        using var db = new AnalyticsDbContext();
        var results = await QueryHelper.ExecuteAsync(db.LedgerAnalytics, request);
        return JsonSerializer.Serialize(results);
    }

    // ── Prompt ───────────────────────────────────────────────────────────

    [McpServerPrompt(Name = "logging-retention-audit", Title = "Logging Retention Audit")]
    [Description("Strategy for auditing log retention, pipeline health, and telemetry coverage.")]
    public static string LoggingRetentionAudit() =>
        """
        You are a logging pipeline auditor. Follow this strategy:
        1. Call query_ledger_analytics filtering IsRetentionCompliant eq false to find sources violating retention policy.
        2. Call query_ledger_analytics filtering GapDetected eq true to find sources with missing data windows.
        3. Call query_ledger_analytics filtering IsBacklogBreach eq true to find shipping pipelines that are falling behind.
        4. Call query_ledger_analytics ordered by GrowthRateBytesPerHour descending, top 10, to find the fastest-growing log sources.
        5. Call query_logging_inventory ordered by SizeBytes descending, top 10, to identify the largest sources and cross-reference with growth rates.
        6. Call query_logging_metrics filtering Status ne OK to find pipeline components in a degraded state.
        7. Correlate: a non-compliant source with a detected gap and a backlog breach is an S2. A fast-growing source approaching retention limits is an S3. Classify and FLAG accordingly.
        """;
}
