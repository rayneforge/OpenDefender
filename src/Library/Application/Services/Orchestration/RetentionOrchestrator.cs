using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Library.Infrastructure.Database;

namespace Library.Application.Services.Orchestration;

/// <summary>
/// Purges stale rows from both the Report and Analytics databases
/// based on a configurable retention window.
/// </summary>
public class RetentionOrchestrator
{
    private readonly DateTime _cutoff;

    /// <param name="cutoff">Any row with a Timestamp before this value will be deleted.</param>
    public RetentionOrchestrator(DateTime cutoff)
    {
        _cutoff = cutoff;
    }

    /// <summary>
    /// Purge stale data from both databases. Returns the total number of rows removed.
    /// </summary>
    public async Task<int> RunAsync()
    {
        var totalRemoved = 0;

        totalRemoved += await PurgeReportDataAsync();
        totalRemoved += await PurgeAnalyticsDataAsync();

        return totalRemoved;
    }

    private async Task<int> PurgeReportDataAsync()
    {
        using var db = new ReportDbContext();
        var removed = 0;

        // State
        removed += await db.Orchestrations
            .Where(x => x.StartTime < _cutoff)
            .ExecuteDeleteAsync();

        // Metrics
        removed += await db.AutomationMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.ControlMapMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.DataRecoveryMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.GpuMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.HardwareMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.KernelMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.LoggingMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.LoggingInventoryMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.NetworkingMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.PacketTracingMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.ResourceMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.SecurityChecks
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.ServiceMetrics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        return removed;
    }

    private async Task<int> PurgeAnalyticsDataAsync()
    {
        using var db = new AnalyticsDbContext();
        var removed = 0;

        removed += await db.ResourceAnalytics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.SecurityAnalytics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.ReliabilityAnalytics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        removed += await db.LedgerAnalytics
            .Where(x => x.Timestamp < _cutoff)
            .ExecuteDeleteAsync();

        return removed;
    }
}
