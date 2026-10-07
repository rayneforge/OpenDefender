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
/// Shield tools — security posture, network perimeter, traffic integrity.
/// </summary>
public static class SecurityTools
{
    public static IEnumerable<AITool> GetTools()
    {
        yield return AIFunctionFactory.Create(QuerySecurityChecks, "query_security_checks", "Query raw security checks. Properties: Id (int), Timestamp (DateTime), CheckType (string), Item (string), Result (string), Value (double), Severity (string). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryNetworkingMetrics, "query_networking_metrics", "Query raw networking metrics. Properties: Id (int), Timestamp (DateTime), Interface (string), Metric (string), Value (double), Status (string). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryNetworkConnections, "query_network_connections", "Read a live TCP connection snapshot: local and remote addresses, ports, and state. No packet payload or snapshot is stored. Empty results do not prove absence of traffic; UDP and past connections are outside scope.");
        yield return AIFunctionFactory.Create(QueryPacketTracing, "query_packet_tracing", "Query raw packet tracing captures. Properties: Id (int), Timestamp (DateTime), Interface (string), PacketsCaptured (int), Status (string). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QuerySecurityAnalytics, "query_security_analytics", "Query derived security analytics. Properties: Id (int), Timestamp (DateTime), CheckType (string), NewIssuesCount (int), IsBreach (bool), Severity (string). Supports structured filters, ordering, and paging.");
    }

    public static async Task<string> QuerySecurityChecks(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.SecurityChecks, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryNetworkingMetrics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.NetworkingMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryPacketTracing(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.PacketTracingMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryNetworkConnections(QueryRequest request, CancellationToken ct = default)
    {
        var snapshot = await new Library.Application.Services.Collectors.NetworkConnectionCollector().CollectAsync(ct: ct);
        var results = QueryHelper.Apply(snapshot.AsQueryable(), request).ToList();
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QuerySecurityAnalytics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new AnalyticsDbContext();
        var results = await QueryHelper.ExecuteAsync(db.SecurityAnalytics, request);
        return JsonSerializer.Serialize(results);
    }
}
