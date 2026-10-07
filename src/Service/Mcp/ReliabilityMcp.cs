using System.ComponentModel;
using ModelContextProtocol.Server;
using Library.Domain.Models.Queries;
using Library.Application.Tooling;

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

    [McpServerTool(Name = "query_data_recovery", Title = "Query Data Recovery Metrics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Query raw data-recovery / backup metrics. Properties: Id (int), Timestamp (DateTime), Source (string), Status (string), SizeBytes (long). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryDataRecovery(QueryRequest request)
    {
        return await ReliabilityTools.QueryDataRecovery(request);
    }

    [McpServerTool(Name = "query_service_metrics", Title = "Query Service Metrics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Query raw service health metrics. Properties: Id (int), Timestamp (DateTime), Service (string), Status (string), UptimeSeconds (double). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryServiceMetrics(QueryRequest request)
    {
        return await ReliabilityTools.QueryServiceMetrics(request);
    }

    [McpServerTool(Name = "query_control_map", Title = "Query Control Map Metrics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Query raw control-map entries. Properties: Id (int), Timestamp (DateTime), Layer (string), Status (string), Signal (string), ActionRequired (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryControlMap(QueryRequest request)
    {
        return await ReliabilityTools.QueryControlMap(request);
    }

    [McpServerTool(Name = "query_automation_metrics", Title = "Query Automation Metrics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Query raw automation / CI-CD metrics. Properties: Id (int), Timestamp (DateTime), Tool (string), Job (string), Result (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryAutomationMetrics(QueryRequest request)
    {
        return await ReliabilityTools.QueryAutomationMetrics(request);
    }

    // ── Derived analytics ───────────────────────────────────────────────

    [McpServerTool(Name = "query_reliability_analytics", Title = "Query Reliability Analytics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Query derived reliability analytics. Properties: Id (int), Timestamp (DateTime), Scope (string), Entity (string), StatusChange (string), IsDegraded (bool), GapDetected (bool). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryReliabilityAnalytics(QueryRequest request)
    {
        return await ReliabilityTools.QueryReliabilityAnalytics(request);
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
