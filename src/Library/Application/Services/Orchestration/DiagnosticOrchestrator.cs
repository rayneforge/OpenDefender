using System;
using System.Linq;
using System.Threading.Tasks;
using Library.Application.Services.Collectors;
using Library.Domain.Abstractions;
using Library.Domain.Models.State;
using Library.Infrastructure.Database;

namespace Library.Application.Services.Orchestration;

/// <summary>
/// Orchestrates all collectors, runs them via live system commands,
/// and persists the results into the ReportDbContext.
/// </summary>
public class DiagnosticOrchestrator : IOrchestrator<OrchestrationState>
{
    private readonly DateTime? _since;
    private readonly bool _enablePacketCapture;

    /// <param name="since">Optional timestamp to scope time-sensitive collectors.</param>
    public DiagnosticOrchestrator(DateTime? since = null, bool enablePacketCapture = false)
    {
        _since = since;
        _enablePacketCapture = enablePacketCapture;
    }

    public Task RunAsync(OrchestrationState state) => RunAsync(state, CancellationToken.None);

    public async Task RunAsync(OrchestrationState state, CancellationToken ct)
    {
        using var db = new ReportDbContext();
        await db.Database.EnsureCreatedAsync(ct);

        // Track the orchestration run
        db.Orchestrations.Add(state);

        // Run all 13 collectors against live system commands
        var automation = await new AutomationCollector().CollectAsync(_since, ct);
        db.AutomationMetrics.AddRange(automation);

        var controlMap = await new ControlMapCollector().CollectAsync(_since, ct);
        db.ControlMapMetrics.AddRange(controlMap);

        var dataRecovery = await new DataRecoveryCollector().CollectAsync(_since, ct);
        db.DataRecoveryMetrics.AddRange(dataRecovery);

        var gpu = await new GpuCollector().CollectAsync(_since, ct);
        db.GpuMetrics.AddRange(gpu);

        var hardware = await new HardwareCollector().CollectAsync(_since, ct);
        db.HardwareMetrics.AddRange(hardware);

        var kernel = await new KernelCollector().CollectAsync(_since, ct);
        db.KernelMetrics.AddRange(kernel);

        var logging = (await new LoggingCollector().CollectAsync(_since, ct)).ToList();
        logging.ForEach(x => x.Timestamp = state.StartTime);
        db.LoggingMetrics.AddRange(logging);

        var loggingInventory = (await new LoggingInventoryCollector().CollectAsync(_since, ct)).ToList();
        loggingInventory.ForEach(x => x.Timestamp = state.StartTime);
        db.LoggingInventoryMetrics.AddRange(loggingInventory);

        var networking = await new NetworkingCollector().CollectAsync(_since, ct);
        db.NetworkingMetrics.AddRange(networking);

        if (_enablePacketCapture)
        {
            var packetTracing = await new PacketTracingCollector().CollectAsync(_since, ct);
            db.PacketTracingMetrics.AddRange(packetTracing);
        }

        var resources = (await new ResourceCollector().CollectAsync(_since, ct)).ToList();
        resources.ForEach(x => x.Timestamp = state.StartTime);
        db.ResourceMetrics.AddRange(resources);

        var security = (await new SecurityCollector().CollectAsync(_since, ct)).ToList();
        security.ForEach(x => x.Timestamp = state.StartTime);
        db.SecurityChecks.AddRange(security);

        var services = (await new ServiceCollector().CollectAsync(_since, ct)).ToList();
        services.ForEach(x => x.Timestamp = state.StartTime);
        db.ServiceMetrics.AddRange(services);

        await db.SaveChangesAsync(ct);
    }
}
