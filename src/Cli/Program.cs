using System;
using System.Linq;
using System.Threading.Tasks;
using Library.Application.Services.Orchestration;
using Library.Domain.Models.State;
using Library.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("═ Diagnostic CLI ═");
Console.WriteLine("Initializing diagnostic collection...");

var state = new OrchestrationState
{
    RunId = Guid.NewGuid(),
    StartTime = DateTime.UtcNow
};

// Use last 24h as default lookback
var lookback = DateTime.UtcNow.AddHours(-24);
var orchestrator = new DiagnosticOrchestrator(since: lookback);

try 
{
    // 1. Run Diagnostic Collection
    await orchestrator.RunAsync(state);
    Console.WriteLine($"\n✓ Diagnostic Orchestration complete — RunId: {state.RunId}");
    
    // 2. Run Analytics Processing
    Console.WriteLine("Initializing analytics processing...");
    var analyticsOrchestrator = new AnalyticsOrchestrator();
    await analyticsOrchestrator.RunAsync(state);
    Console.WriteLine($"✓ Analytics Orchestration complete");

    // 3. Report Results
    using var db = new ReportDbContext();
    using var analyticsDb = new AnalyticsDbContext();
    
    Console.WriteLine("\n📊 Raw Metric Counts:");
    Console.WriteLine($"  Kernel:       {await db.KernelMetrics.CountAsync()}");
    Console.WriteLine($"  Resources:    {await db.ResourceMetrics.CountAsync()}");
    Console.WriteLine($"  Security:     {await db.SecurityChecks.CountAsync()}");
    Console.WriteLine($"  Hardware:     {await db.HardwareMetrics.CountAsync()}");
    Console.WriteLine($"  GPU:          {await db.GpuMetrics.CountAsync()}");
    Console.WriteLine($"  Services:     {await db.ServiceMetrics.CountAsync()}");
    Console.WriteLine($"  Network:      {await db.NetworkingMetrics.CountAsync()}");
    Console.WriteLine($"  Automation:   {await db.AutomationMetrics.CountAsync()}");
    Console.WriteLine($"  Logging Inv:  {await db.LoggingInventoryMetrics.CountAsync()}");
    Console.WriteLine($"  Orchestrations: {await db.Orchestrations.CountAsync()}");

    Console.WriteLine("\n🧠 Derived Analytics Counts:");
    Console.WriteLine($"  Resource Analytics:    {await analyticsDb.ResourceAnalytics.CountAsync()}");
    Console.WriteLine($"  Security Analytics:    {await analyticsDb.SecurityAnalytics.CountAsync()}");
    Console.WriteLine($"  Reliability Analytics: {await analyticsDb.ReliabilityAnalytics.CountAsync()}");
    Console.WriteLine($"  Ledger Analytics:      {await analyticsDb.LedgerAnalytics.CountAsync()}");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error during orchestration: {ex.Message}");
}
