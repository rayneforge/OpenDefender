using System.ComponentModel;
using ModelContextProtocol.Server;
using Library.Domain.Models.Queries;
using Library.Infrastructure.Database;
using Library.Infrastructure.Query;
using System.Text.Json;

namespace Service.Mcp;

/// <summary>
/// Shield domain — security posture, network perimeter, traffic integrity.
/// Raw: SecurityChecks, NetworkingMetrics, PacketTracingMetrics.
/// Derived: SecurityAnalytics.
/// </summary>
[McpServerToolType]
[McpServerPromptType]
public static class SecurityMcp
{
    // ── Raw metrics ─────────────────────────────────────────────────────

    [McpServerTool(Name = "query_security_checks", Title = "Query Security Checks")]
    [Description("Query raw security checks. Properties: Id (int), Timestamp (DateTime), CheckType (string), Item (string), Result (string), Value (double), Severity (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QuerySecurityChecks(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.SecurityChecks, request);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "query_networking_metrics", Title = "Query Networking Metrics")]
    [Description("Query raw networking metrics. Properties: Id (int), Timestamp (DateTime), Interface (string), Metric (string), Value (double), Status (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryNetworkingMetrics(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.NetworkingMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "query_packet_tracing", Title = "Query Packet Tracing")]
    [Description("Query raw packet tracing captures. Properties: Id (int), Timestamp (DateTime), Interface (string), PacketsCaptured (int), Status (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryPacketTracing(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.PacketTracingMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    // ── Derived analytics ───────────────────────────────────────────────

    [McpServerTool(Name = "query_security_analytics", Title = "Query Security Analytics")]
    [Description("Query derived security analytics. Properties: Id (int), Timestamp (DateTime), CheckType (string), NewIssuesCount (int), IsBreach (bool), Severity (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QuerySecurityAnalytics(QueryRequest request)
    {
        using var db = new AnalyticsDbContext();
        var results = await QueryHelper.ExecuteAsync(db.SecurityAnalytics, request);
        return JsonSerializer.Serialize(results);
    }

    // ── Prompt ───────────────────────────────────────────────────────────

    [McpServerPrompt(Name = "security-posture-assessment", Title = "Security Posture Assessment")]
    [Description("Strategy for assessing the security posture of the system.")]
    public static string SecurityPostureAssessment() =>
        """
        You are a security analyst assessing system posture. Follow this strategy:
        1. Call query_security_analytics with IsBreach eq true to identify active breaches and their severity.
        2. Call query_security_checks, ordered by Timestamp descending, to review the latest raw findings across CheckType values (Firewall, Auth, Network).
        3. Call query_networking_metrics filtering Status ne OK to find degraded interfaces.
        4. Call query_packet_tracing filtering Status ne OK to find anomalous capture sessions.
        5. Correlate: map raw check failures to analytics breaches. Flag any CheckType with rising NewIssuesCount.
        6. Classify findings as S1-S4 per severity definitions. Produce a FLAG for anything S2 or above.
        """;
}
