using System;
using System.Threading.Tasks;
using Library.Application.Services.Orchestration;
using Library.Domain.Attributes;
using Library.Domain.Models.State;
using Microsoft.Extensions.Logging;

namespace Library.Application.Consumers;

/// <summary>
/// Consumes the <c>diagnostic_collection</c> event and runs the
/// <see cref="AnalyticsOrchestrator"/> against the completed run.
/// </summary>
public class AnalyticsConsumer
{
    private readonly ILogger<AnalyticsConsumer> _logger;

    public AnalyticsConsumer(ILogger<AnalyticsConsumer> logger)
    {
        _logger = logger;
    }

    [TaskConsumer("diagnostic_collection")]
    public async Task RunAsync(OrchestrationState state)
    {
        _logger.LogInformation("Analytics consumer triggered for RunId: {Id}", state.RunId);

        try
        {
            var orchestrator = new AnalyticsOrchestrator();
            await orchestrator.RunAsync(state);

            _logger.LogInformation("Analytics run complete for RunId: {Id}", state.RunId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during analytics run for RunId: {Id}", state.RunId);
            throw;
        }
    }
}
