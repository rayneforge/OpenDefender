using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Library.Domain.Abstractions;
using Library.Domain.Models.State;
using Library.Infrastructure.Database; // Covers both ReportDbContext and AnalyticsDbContext
using Library.Domain.Models.Analytics;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Orchestration;

public class AnalyticsOrchestrator : IOrchestrator<OrchestrationState?>
{
    public async Task RunAsync(OrchestrationState? state = null)
    {
        using var rawDb = new ReportDbContext();
        using var analyticsDb = new AnalyticsDbContext();
        
        await analyticsDb.Database.EnsureCreatedAsync();

        // Get the latest raw run
        var latestRawRun = state ?? await rawDb.Orchestrations
            .OrderByDescending(x => x.StartTime)
            .FirstOrDefaultAsync();

        if (latestRawRun == null) return;
        
        var runTime = latestRawRun.StartTime;

        // Check if we've already processed this timestamp
        var existing = await analyticsDb.ResourceAnalytics
            .AnyAsync(x => x.Timestamp == runTime);
            
        if (existing) return;

        // --- Resource Analysis ---
        var currentResources = await rawDb.ResourceMetrics
            .Where(x => x.Timestamp == runTime)
            .ToListAsync();

        var previousRawRun = await rawDb.Orchestrations
            .Where(x => x.StartTime < runTime)
            .OrderByDescending(x => x.StartTime)
            .FirstOrDefaultAsync();

        var previousResources = previousRawRun != null
            ? await rawDb.ResourceMetrics.Where(x => x.Timestamp == previousRawRun.StartTime).ToListAsync()
            : new List<ResourceMetric>();

        foreach (var current in currentResources)
        {
            var prev = previousResources.FirstOrDefault(x => x.Metric == current.Metric);
            double delta = prev != null ? current.Value - prev.Value : 0;
            
            analyticsDb.ResourceAnalytics.Add(new ResourceAnalytics
            {
                Timestamp = runTime,
                Metric = current.Metric,
                CurrentValue = current.Value,
                Delta = delta,
                Rate = 0, // TODO: Calc rate based on time diff
                RateDelta = 0, // TODO: Calc acceleration
                IsBreach = current.Value > current.Threshold,
                Severity = current.Value > current.Threshold ? "High" : "Low"
            });
        }

        // --- Security Analysis ---
        var currentSecurity = await rawDb.SecurityChecks
            .Where(x => x.Timestamp == runTime)
            .ToListAsync();

        foreach (var check in currentSecurity)
        {
            bool isBreach = false;
            // Breach if Firewall Status is not Active
            if (check.CheckType == "Firewall" && check.Item == "Status" && check.Result != "Active") 
                isBreach = true;
            
            // Breach if Auth failed logins > threshold (e.g. 5)
            if (check.CheckType == "Auth" && check.Item == "Failed_Logins" && check.Value > 5) 
                isBreach = true; 

            // Breach if OpenPorts > threshold (e.g. 20) - Example rule
            if (check.CheckType == "Network" && check.Item == "OpenPorts_Count" && check.Value > 20)
                isBreach = true;
            
            if (isBreach || check.Severity == "Critical")
            {
                analyticsDb.SecurityAnalytics.Add(new SecurityAnalytics
                {
                    Timestamp = runTime,
                    CheckType = check.CheckType,
                    NewIssuesCount = (int)check.Value,
                    IsBreach = isBreach,
                    Severity = check.Severity ?? "High"
                });
            }
        }

        // --- Reliability Analysis (Services) ---
        var currentServices = await rawDb.ServiceMetrics
            .Where(x => x.Timestamp == runTime)
            .ToListAsync();
            
        var prevServices = previousRawRun != null
            ? await rawDb.ServiceMetrics.Where(x => x.Timestamp == previousRawRun.StartTime).ToListAsync()
            : new List<ServiceMetric>();

        foreach (var svc in currentServices)
        {
            var prev = prevServices.FirstOrDefault(s => s.Service == svc.Service);
            bool isDegraded = svc.Status != "active";
            bool gapDetected = false;
            
            // Check for restart (Uptime dropped) which implies a gap/failure
            if (prev != null && svc.UptimeSeconds < prev.UptimeSeconds) 
            {
                isDegraded = true; 
                gapDetected = true;
            }

            if (isDegraded)
            {
                analyticsDb.ReliabilityAnalytics.Add(new ReliabilityAnalytics
                {
                    Timestamp = runTime,
                    Scope = "Service",
                    Entity = svc.Service,
                    StatusChange = prev != null ? $"{prev.Status} -> {svc.Status}" : svc.Status,
                    IsDegraded = true,
                    GapDetected = gapDetected
                });
            }
        }

        // --- Ledger Analysis (Logging) ---
        var currentInventory = await rawDb.LoggingInventoryMetrics
            .Where(x => x.Timestamp == runTime)
            .ToListAsync();
            
        var prevInventory = previousRawRun != null
            ? await rawDb.LoggingInventoryMetrics.Where(x => x.Timestamp == previousRawRun.StartTime).ToListAsync()
            : new List<LoggingInventoryMetric>();

        var loggingMetrics = await rawDb.LoggingMetrics
            .Where(x => x.Timestamp == runTime)
            .ToListAsync();

        foreach (var inv in currentInventory)
        {
            var prev = prevInventory.FirstOrDefault(i => i.LogSource == inv.LogSource && i.LogType == inv.LogType);
            
            // 1. Journal Growth Rate
            long growth = prev != null ? inv.SizeBytes - prev.SizeBytes : 0;
            // Assuming hourly runs for rate, but we can refine if timestamp diff is available
            // If delta is huge, it's a spike.
            
            // 2. Retention Compliance (180d Auth / 30d Perf)
            // We don't have explicit "Age" in inventory, but let's assume if status is "Full" or similar it might be an issue.
            // Or look for a companion metric.
            // For now, let's assume compliance is OK unless flagged otherwise by a specific metric.
            bool isCompliant = true;
            double actualRetention = 0; // Unknown
            
            // Check specific LoggingMetrics for this component if available
            var retentionMetric = loggingMetrics.FirstOrDefault(m => m.Component == inv.LogSource && m.Metric == "RetentionDays");
            if (retentionMetric != null)
            {
                actualRetention = retentionMetric.Value;
                if (inv.LogType.Equals("Auth", StringComparison.OrdinalIgnoreCase) && actualRetention < 180) isCompliant = false;
                if (inv.LogType.Equals("Perf", StringComparison.OrdinalIgnoreCase) && actualRetention < 30) isCompliant = false;
            }

            // 3. Coverage Gaps
            bool gapDetected = inv.Status != "Active" && inv.Status != "OK";
            
            // 4. Shipping Backlog
            // Check if there's a backlog metric
            double backlog = 0;
            var backlogMetric = loggingMetrics.FirstOrDefault(m => m.Component == inv.LogSource && m.Metric == "Backlog");
            if (backlogMetric != null) backlog = backlogMetric.Value;

            analyticsDb.LedgerAnalytics.Add(new LedgerAnalytics
            {
                Timestamp = runTime,
                LogSource = inv.LogSource,
                LogType = inv.LogType,
                CurrentSizeBytes = inv.SizeBytes,
                GrowthBytes = growth,
                GrowthRateBytesPerHour = 0, // TODO: Time diff calculation
                IsRetentionCompliant = isCompliant,
                RetentionDays = actualRetention,
                GapDetected = gapDetected,
                ShippingBacklog = backlog,
                IsBacklogBreach = backlog > 1000 // Arbitrary threshold or policy
            });
        }
        
        // Check for missing sources entirely (present in prev, missing in current)
        foreach (var prevItem in prevInventory)
        {
            if (!currentInventory.Any(c => c.LogSource == prevItem.LogSource && c.LogType == prevItem.LogType))
            {
                analyticsDb.LedgerAnalytics.Add(new LedgerAnalytics
                {
                    Timestamp = runTime,
                    LogSource = prevItem.LogSource,
                    LogType = prevItem.LogType,
                    CurrentSizeBytes = 0,
                    GrowthBytes = 0,
                    IsRetentionCompliant = false,
                    GapDetected = true, // Source went missing!
                    ShippingBacklog = 0,
                    IsBacklogBreach = true // Implicitly bad
                });
            }
        }

        await analyticsDb.SaveChangesAsync();
    }
}
