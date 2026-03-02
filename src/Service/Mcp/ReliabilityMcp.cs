using System.ComponentModel;
using ModelContextProtocol.Server;
using Library.Domain.Models.Queries;
using Library.Infrastructure.Database;
using Library.Infrastructure.Query;
using System.Text.Json;

namespace Service.Mcp;

/// <summary>
/// Anchor domain — backup chain, service stability, configuration drift, disaster recovery.
/// Raw: DataRecoveryMetrics, ServiceMetrics, ControlMapMetrics, AutomationMetrics.
/// Derived: ReliabilityAnalytics.
/// </summary>
[McpServerToolType]
[McpServerPromptType]
public static class ReliabilityMcp
{
    // ── Raw metrics ─────────────────────────────────────────────────────

    [McpServerTool(Name = "query_data_recovery", Title = "Query Data Recovery Metrics")]
    [Description("Query raw data-recovery / backup metrics. Properties: Id (int), Timestamp (DateTime), Source (string), Status (string), SizeBytes (long). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryDataRecovery(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.DataRecoveryMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "query_service_metrics", Title = "Query Service Metrics")]
    [Description("Query raw service health metrics. Properties: Id (int), Timestamp (DateTime), Service (string), Status (string), UptimeSeconds (double). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryServiceMetrics(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.ServiceMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "query_control_map", Title = "Query Control Map Metrics")]
    [Description("Query raw control-map entries. Properties: Id (int), Timestamp (DateTime), Layer (string), Status (string), Signal (string), ActionRequired (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryControlMap(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.ControlMapMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "query_automation_metrics", Title = "Query Automation Metrics")]
    [Description("Query raw automation / CI-CD metrics. Properties: Id (int), Timestamp (DateTime), Tool (string), Job (string), Result (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryAutomationMetrics(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.AutomationMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    // ── Derived analytics ───────────────────────────────────────────────

    [McpServerTool(Name = "query_reliability_analytics", Title = "Query Reliability Analytics")]
    [Description("Query derived reliability analytics. Properties: Id (int), Timestamp (DateTime), Scope (string), Entity (string), StatusChange (string), IsDegraded (bool), GapDetected (bool). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryReliabilityAnalytics(QueryRequest request)
    {
        using var db = new AnalyticsDbContext();
        var results = await QueryHelper.ExecuteAsync(db.ReliabilityAnalytics, request);
        return JsonSerializer.Serialize(results);
    }

    // ── Prompt ───────────────────────────────────────────────────────────

    [McpServerPrompt(Name = "reliability-stability-review", Title = "Reliability & Stability Review")]
    [Description("Strategy for reviewing system reliability, backup integrity, and service stability.")]
    public static string ReliabilityStabilityReview() =>
        """
        You are an SRE reviewing system reliability. Follow this strategy:
        1. Call query_reliability_analytics filtering IsDegraded eq true or GapDetected eq true to find active degradations.
        2. Call query_service_metrics ordered by UptimeSeconds ascending to find recently-restarted or unstable services.
        3. Call query_data_recovery filtering Status ne OK to find backup failures or incomplete chains.
        4. Call query_control_map filtering ActionRequired ne None to find controls requiring intervention.
        5. Call query_automation_metrics filtering Result ne Success to find failing CI/CD jobs.
        6. Correlate: a degraded service with a failing backup and a triggered control map entry is an S2. Produce a FLAG for each finding S3 or above.
        """;
}
