using Microsoft.EntityFrameworkCore;
using Library.Domain.Models.State;
using Library.Domain.Models.Metrics;

namespace Library.Infrastructure.Database;

public class ReportDbContext : DbContext
{
    // State
    public DbSet<OrchestrationState> Orchestrations { get; set; }
    public DbSet<AgentTask> AgentTasks { get; set; }

    // Metrics (1-to-1 with CSV samples)
    public DbSet<AutomationMetric> AutomationMetrics { get; set; }
    public DbSet<ControlMapMetric> ControlMapMetrics { get; set; }
    public DbSet<DataRecoveryMetric> DataRecoveryMetrics { get; set; }
    public DbSet<GpuMetric> GpuMetrics { get; set; }
    public DbSet<HardwareMetric> HardwareMetrics { get; set; }
    public DbSet<KernelMetric> KernelMetrics { get; set; }
    public DbSet<LoggingMetric> LoggingMetrics { get; set; }
    public DbSet<LoggingInventoryMetric> LoggingInventoryMetrics { get; set; }
    public DbSet<NetworkingMetric> NetworkingMetrics { get; set; }
    public DbSet<PacketTracingMetric> PacketTracingMetrics { get; set; }
    public DbSet<ResourceMetric> ResourceMetrics { get; set; }
    public DbSet<SecurityCheck> SecurityChecks { get; set; }
    public DbSet<ServiceMetric> ServiceMetrics { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = DatabasePaths.GetPath("diagnostic_reports.db")
        };
        optionsBuilder.UseSqlite(connection.ConnectionString);
    }
}
