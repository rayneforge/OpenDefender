using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Library.Application.Services.Orchestration;
using Library.Domain.Models.State;

namespace Service.Services;

/// <summary>
/// Hosted service that runs the DiagnosticOrchestrator periodically.
/// </summary>
public class DiagnosticHostedService : BackgroundService
{
    private readonly ILogger<DiagnosticHostedService> _logger;
    private readonly ServiceOptions _options;

    public DiagnosticHostedService(
        ILogger<DiagnosticHostedService> logger, 
        IOptions<ServiceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DiagnosticHostedService is starting. Interval: {interval}", _options.CollectionFrequency);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Scheduled diagnostic run started: {time}", DateTimeOffset.Now);

                var state = new OrchestrationState
                {
                    RunId = Guid.NewGuid(),
                    StartTime = DateTime.UtcNow
                };

                // Initialize orchestrator with lookback from options
                var since = DateTime.UtcNow.AddHours(-_options.LookbackHours);
                var orchestrator = new DiagnosticOrchestrator(since: since);

                await orchestrator.RunAsync(state);

                _logger.LogInformation("Scheduled diagnostic run complete. RunId: {Id}", state.RunId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during scheduled diagnostic collection.");
            }

            await Task.Delay(_options.CollectionFrequency, stoppingToken);
        }
    }
}
