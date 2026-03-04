using System;
using System.Threading.Tasks;
using Library.Application.Services.Orchestration;
using Library.Domain.Attributes;
using Library.Domain.Models.State;
using Microsoft.Extensions.Options;

namespace Library.Application.Producers;

/// <summary>
/// Scheduled producer for retention purge.
/// The <see cref="TaskSchedulerService"/> invokes this on its interval,
/// and the result is published to the channel for any downstream consumers.
/// </summary>
public class RetentionProducer
{
    private readonly ServiceOptions _options;

    public RetentionProducer(IOptions<ServiceOptions> options)
    {
        _options = options.Value;
    }

    [TaskTrigger("retention_purge", "Purge stale data from report and analytics databases.", "00:30:00")]
    public async Task<int> RunAsync()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-_options.RetentionMinutes);
        var orchestrator = new RetentionOrchestrator(cutoff);

        return await orchestrator.RunAsync();
    }
}
