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
/// Hosted service that runs the AnalyticsOrchestrator periodically.
/// Runs *after* the raw collection typically, but here we just run on a schedule.
/// </summary>
public class AnalyticsHostedService : BackgroundService
{
    private readonly ILogger<AnalyticsHostedService> _logger;
    private readonly ServiceOptions _options;

    public AnalyticsHostedService(
        ILogger<AnalyticsHostedService> logger, 
        IOptions<ServiceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AnalyticsHostedService is starting. Interval: {interval}", _options.CollectionFrequency);

        // Delay slightly to let raw collector run first if they share start time
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Scheduled analytics run started: {time}", DateTimeOffset.Now);

                var orchestrator = new AnalyticsOrchestrator();

                // Pass null to let orchestrator find the latest run automatically
                await orchestrator.RunAsync(null);

                _logger.LogInformation("Scheduled analytics run complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing analytics run.");
            }

            // Wait for the next cycle
            // Use same frequency as collection for now
            if (_options.CollectionFrequency > TimeSpan.Zero)
            {
                await Task.Delay(_options.CollectionFrequency, stoppingToken);
            }
            else
            {
                // Default delay if invalid config
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
