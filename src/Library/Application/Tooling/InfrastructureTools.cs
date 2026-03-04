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
/// Infrastructure tools — hardware, kernel/OS, resource performance, GPU/accelerators.
/// </summary>
public static class InfrastructureTools
{
    public static IEnumerable<AITool> GetTools()
    {
        yield return AIFunctionFactory.Create(QueryResourceMetrics, "query_resource_metrics", "Query raw resource metrics. Properties: Id (int), Timestamp (DateTime), Metric (string), Value (double), Threshold (double). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryHardwareMetrics, "query_hardware_metrics", "Query raw hardware metrics. Properties: Id (int), Timestamp (DateTime), Device (string), Attribute (string), Value (double), Status (string). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryKernelMetrics, "query_kernel_metrics", "Query raw kernel / OS metrics. Properties: Id (int), Timestamp (DateTime), Category (string), Metric (string), Value (string), Alert (string). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryGpuMetrics, "query_gpu_metrics", "Query raw GPU / accelerator metrics. Properties: Id (int), Timestamp (DateTime), Vendor (string), Device (string), GpuUtil (double), MemUtil (double), Temp (double). Supports structured filters, ordering, and paging.");
        yield return AIFunctionFactory.Create(QueryResourceAnalytics, "query_resource_analytics", "Query derived resource analytics. Properties: Id (int), Timestamp (DateTime), Metric (string), CurrentValue (double), Delta (double), Rate (double), RateDelta (double), IsBreach (bool), Severity (string). Supports structured filters, ordering, and paging.");
    }

    public static async Task<string> QueryResourceMetrics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.ResourceMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryHardwareMetrics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.HardwareMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryKernelMetrics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.KernelMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryGpuMetrics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.GpuMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    public static async Task<string> QueryResourceAnalytics(QueryRequest request, CancellationToken ct = default)
    {
        using var db = new AnalyticsDbContext();
        var results = await QueryHelper.ExecuteAsync(db.ResourceAnalytics, request);
        return JsonSerializer.Serialize(results);
    }
}
