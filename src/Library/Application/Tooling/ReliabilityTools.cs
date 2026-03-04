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
/// Reliability tools — backup chain, service stability, configuration drift, disaster recovery.
/// </summary>
public static class ReliabilityTools
{
    public static IEnumerable<AITool> GetTools()
    {
        yield return AIFunctionFactory.Create(QueryDataRecovery, "query_data_recovery", "Query raw data-recovery / backup metrics. Properties: Id (int), Timestamp (DateTime), Source (string), Status (string), SizeBytes (long). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryServiceMetrics, "query_service_metrics", "Query raw service health metrics. Properties: Id (int), Timestamp (DateTime), Service (string), Status (string), UptimeSeconds (double). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryControlMap, "query_control_map", "Query raw control-map entries. Properties: Id (int), Timestamp (DateTime), Layer (string), Status (string), Signal (string), ActionRequired (string). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryAutomationMetrics, "query_automation_metrics", "Query raw automation / CI-CD metrics. Properties: Id (int), Timestamp (DateTime), Tool (string), Job (string), Result (string). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryReliabilityAnalytics, "query_reliability_analytics", "Query derived reliability analytics. Properties: Id (int), Timestamp (DateTime), Scope (string), Entity (string), StatusChange (string), IsDegraded (bool), GapDetected (bool). Supports structured filters, ordering, and paging.");
    }

    public static async Task<string> QueryDataRecovery(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.DataRecoveryMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryServiceMetrics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.ServiceMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryControlMap(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.ControlMapMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryAutomationMetrics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.AutomationMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryReliabilityAnalytics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new AnalyticsDbContext();
        var results = await QueryHelper.ExecuteAsync(db.ReliabilityAnalytics, request);
        return JsonSerializer.Serialize(results);
    }
}
