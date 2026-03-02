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
/// Hosted service that periodically purges stale data from both databases
/// based on the configured retention window.
/// </summary>
public class RetentionHostedService : BackgroundService
{
    private readonly ILogger<RetentionHostedService> _logger;
    private readonly ServiceOptions _options;

    public RetentionHostedService(
        ILogger<RetentionHostedService> logger,
        IOptions<ServiceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RetentionHostedService started. Retention: {minutes}m, Frequency: {freq}",
            _options.RetentionMinutes, _options.RetentionFrequency);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-_options.RetentionMinutes);
                _logger.LogInformation("Retention purge started. Cutoff: {cutoff}", cutoff);

                var orchestrator = new RetentionOrchestrator(cutoff);
                var removed = await orchestrator.RunAsync();

                _logger.LogInformation("Retention purge complete. Rows removed: {count}", removed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during retention purge.");
            }

            await Task.Delay(_options.RetentionFrequency, stoppingToken);
        }
    }
}
