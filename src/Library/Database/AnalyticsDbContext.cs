using Microsoft.EntityFrameworkCore;
using Library.Domain.Models.Analytics;

namespace Library.Infrastructure.Database;

public class AnalyticsDbContext : DbContext
{
    public DbSet<ResourceAnalytics> ResourceAnalytics { get; set; }
    public DbSet<SecurityAnalytics> SecurityAnalytics { get; set; }
    public DbSet<ReliabilityAnalytics> ReliabilityAnalytics { get; set; }
    public DbSet<LedgerAnalytics> LedgerAnalytics { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=analytics_reports.db");
    }
}
