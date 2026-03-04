using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Library.Domain.Models.Queries;
using Library.Infrastructure.Database;
using Library.Infrastructure.Query;

namespace Library.Application.Tooling;

/// <summary>
/// Logging tools — log retention, telemetry shipping, coverage, pipeline health.
/// </summary>
public static class LoggingTools
{
    public static IEnumerable<AITool> GetTools()
    {
        yield return AIFunctionFactory.Create(QueryLoggingMetrics, "query_logging_metrics", "Query raw logging pipeline metrics. Properties: Id (int), Timestamp (DateTime), Component (string), Metric (string), Value (double), Status (string). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryLoggingInventory, "query_logging_inventory", "Query raw log source inventory. Properties: Id (int), Timestamp (DateTime), LogSource (string), LogType (string), SizeBytes (long), Status (string). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryLedgerAnalytics, "query_ledger_analytics", "Query derived ledger analytics. Properties: Id (int), Timestamp (DateTime), LogSource (string), LogType (string), CurrentSizeBytes (long), GrowthBytes (long), GrowthRateBytesPerHour (double), IsRetentionCompliant (bool), RetentionDays (double), GapDetected (bool), ShippingBacklog (double), IsBacklogBreach (bool). Supports structured filters, ordering, and paging.");
    }

    public static async Task<string> QueryLoggingMetrics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.LoggingMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryLoggingInventory(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.LoggingInventoryMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryLedgerAnalytics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new AnalyticsDbContext();
        var results = await QueryHelper.ExecuteAsync(db.LedgerAnalytics, request);
        return JsonSerializer.Serialize(results);
    }
}
