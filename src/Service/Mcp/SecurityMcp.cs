using System.ComponentModel;
using ModelContextProtocol.Server;
using Library.Domain.Models.Queries;
using Library.Application.Tooling;

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

    [McpServerTool(Name = "query_security_checks", Title = "Query Security Checks", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Query raw security checks. Properties: Id (int), Timestamp (DateTime), CheckType (string), Item (string), Result (string), Value (double), Severity (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QuerySecurityChecks(QueryRequest request)
    {
        return await SecurityTools.QuerySecurityChecks(request);
    }

    [McpServerTool(Name = "query_networking_metrics", Title = "Query Networking Metrics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Query raw networking metrics. Properties: Id (int), Timestamp (DateTime), Interface (string), Metric (string), Value (double), Status (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryNetworkingMetrics(QueryRequest request)
    {
        return await SecurityTools.QueryNetworkingMetrics(request);
    }

    [McpServerTool(Name = "query_packet_tracing", Title = "Query Packet Tracing", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Query raw packet tracing captures. Properties: Id (int), Timestamp (DateTime), Interface (string), PacketsCaptured (int), Status (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryPacketTracing(QueryRequest request)
    {
        return await SecurityTools.QueryPacketTracing(request);
    }

    // ── Derived analytics ───────────────────────────────────────────────

    [McpServerTool(Name = "query_network_connections", Title = "Inspect TCP Connections", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Read current TCP connections and listeners with local/remote addresses, ports, state, and Timestamp. Live snapshot; no packets or endpoint history are stored. Supports structured filters, ordering, paging. Does not measure UDP traffic, bytes per peer, connection initiation direction, or historical traffic. Unavailable inspection returns an error, not a healthy result.")]
    public static Task<string> QueryNetworkConnections(QueryRequest request, CancellationToken ct)
        => SecurityTools.QueryNetworkConnections(request, ct);

    [McpServerTool(Name = "query_security_analytics", Title = "Query Security Analytics", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Query derived security analytics. Properties: Id (int), Timestamp (DateTime), CheckType (string), NewIssuesCount (int), IsBreach (bool), Severity (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QuerySecurityAnalytics(QueryRequest request)
    {
        return await SecurityTools.QuerySecurityAnalytics(request);
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
