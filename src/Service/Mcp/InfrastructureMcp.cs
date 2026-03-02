using System.ComponentModel;
using ModelContextProtocol.Server;
using Library.Domain.Models.Queries;
using Library.Infrastructure.Database;
using Library.Infrastructure.Query;
using System.Text.Json;

namespace Service.Mcp;

/// <summary>
/// Core domain — hardware, kernel/OS, resource performance, GPU/accelerators.
/// Raw: ResourceMetrics, HardwareMetrics, KernelMetrics, GpuMetrics.
/// Derived: ResourceAnalytics.
/// </summary>
[McpServerToolType]
[McpServerPromptType]
public static class InfrastructureMcp
{
    // ── Raw metrics ─────────────────────────────────────────────────────

    [McpServerTool(Name = "query_resource_metrics", Title = "Query Resource Metrics")]
    [Description("Query raw resource metrics. Properties: Id (int), Timestamp (DateTime), Metric (string), Value (double), Threshold (double). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryResourceMetrics(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.ResourceMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "query_hardware_metrics", Title = "Query Hardware Metrics")]
    [Description("Query raw hardware metrics. Properties: Id (int), Timestamp (DateTime), Device (string), Attribute (string), Value (double), Status (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryHardwareMetrics(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.HardwareMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "query_kernel_metrics", Title = "Query Kernel Metrics")]
    [Description("Query raw kernel / OS metrics. Properties: Id (int), Timestamp (DateTime), Category (string), Metric (string), Value (string), Alert (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryKernelMetrics(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.KernelMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    [McpServerTool(Name = "query_gpu_metrics", Title = "Query GPU Metrics")]
    [Description("Query raw GPU / accelerator metrics. Properties: Id (int), Timestamp (DateTime), Vendor (string), Device (string), GpuUtil (double), MemUtil (double), Temp (double). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryGpuMetrics(QueryRequest request)
    {
        using var db = new ReportDbContext();
        var results = await QueryHelper.ExecuteAsync(db.GpuMetrics, request);
        return JsonSerializer.Serialize(results);
    }

    // ── Derived analytics ───────────────────────────────────────────────

    [McpServerTool(Name = "query_resource_analytics", Title = "Query Resource Analytics")]
    [Description("Query derived resource analytics. Properties: Id (int), Timestamp (DateTime), Metric (string), CurrentValue (double), Delta (double), Rate (double), RateDelta (double), IsBreach (bool), Severity (string). Supports structured filters, ordering, and paging.")]
    public static async Task<string> QueryResourceAnalytics(QueryRequest request)
    {
        using var db = new AnalyticsDbContext();
        var results = await QueryHelper.ExecuteAsync(db.ResourceAnalytics, request);
        return JsonSerializer.Serialize(results);
    }

    // ── Prompt ───────────────────────────────────────────────────────────

    [McpServerPrompt(Name = "infrastructure-health-check", Title = "Infrastructure Health Check")]
    [Description("Strategy for evaluating hardware, kernel, GPU, and resource performance.")]
    public static string InfrastructureHealthCheck() =>
        """
        You are an infrastructure engineer evaluating system health. Follow this strategy:
        1. Call query_resource_analytics filtering IsBreach eq true to find metrics exceeding thresholds.
        2. Call query_resource_metrics ordered by Timestamp descending, top 20, to see the latest CPU/memory/load readings and compare Value against Threshold.
        3. Call query_hardware_metrics filtering Status ne OK to find devices reporting problems.
        4. Call query_kernel_metrics filtering Alert ne None to find kernel-level warnings.
        5. Call query_gpu_metrics ordered by Temp descending, top 10, to find thermal outliers; also check GpuUtil and MemUtil for saturation.
        6. Correlate: a resource breach on CPU combined with a kernel alert and high GPU utilisation signals a compute-bound S2 event. Classify and FLAG accordingly.
        """;
}
