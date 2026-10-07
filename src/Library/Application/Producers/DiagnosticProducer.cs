using System;
using System.Threading.Tasks;
using Library.Application.Services.Orchestration;
using Library.Domain.Attributes;
using Library.Domain.Models.State;
using Microsoft.Extensions.Options;

namespace Library.Application.Producers;

/// <summary>
/// Scheduled producer for diagnostic collection.
/// The <see cref="TaskSchedulerService"/> invokes this on its interval,
/// and the result is published to the channel for consumers (e.g. analytics).
/// </summary>
public class DiagnosticProducer
{
    private readonly ServiceOptions _options;

    public DiagnosticProducer(IOptions<ServiceOptions> options)
    {
        _options = options.Value;
    }

    [TaskTrigger("diagnostic_collection", "Collect all system diagnostics and persist raw metrics.", "00:15:00")]
    public async Task<OrchestrationState> RunAsync(CancellationToken ct = default)
    {
        var state = new OrchestrationState
        {
            RunId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };

        var since = DateTime.UtcNow.AddHours(-_options.LookbackHours);
        var orchestrator = new DiagnosticOrchestrator(since: since, enablePacketCapture: _options.EnablePacketCapture);

        await orchestrator.RunAsync(state, ct);

        return state;
    }
}
