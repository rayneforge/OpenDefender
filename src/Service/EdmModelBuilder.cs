using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Library.Domain.Models.State;
using Library.Domain.Models.Metrics;
using Library.Domain.Models.Analytics;

namespace Service;

public static class EdmModelBuilder
{
    public static IEdmModel GetReportModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<OrchestrationState>("Orchestrations");
        builder.EntitySet<AutomationMetric>("AutomationMetrics");
        builder.EntitySet<ControlMapMetric>("ControlMapMetrics");
        builder.EntitySet<DataRecoveryMetric>("DataRecoveryMetrics");
        builder.EntitySet<GpuMetric>("GpuMetrics");
        builder.EntitySet<HardwareMetric>("HardwareMetrics");
        builder.EntitySet<KernelMetric>("KernelMetrics");
        builder.EntitySet<LoggingMetric>("LoggingMetrics");
        builder.EntitySet<LoggingInventoryMetric>("LoggingInventoryMetrics");
        builder.EntitySet<NetworkingMetric>("NetworkingMetrics");
        builder.EntitySet<PacketTracingMetric>("PacketTracingMetrics");
        builder.EntitySet<ResourceMetric>("ResourceMetrics");
        builder.EntitySet<SecurityCheck>("SecurityChecks");
        builder.EntitySet<ServiceMetric>("ServiceMetrics");
        return builder.GetEdmModel();
    }

    public static IEdmModel GetAnalyticsModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<ResourceAnalytics>("ResourceAnalytics");
        builder.EntitySet<SecurityAnalytics>("SecurityAnalytics");
        builder.EntitySet<ReliabilityAnalytics>("ReliabilityAnalytics");
        builder.EntitySet<LedgerAnalytics>("LedgerAnalytics");
        return builder.GetEdmModel();
    }
}
