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
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".data", "analytics_reports.db");
        var directory = Path.GetDirectoryName(dbPath);
        if (directory != null && !Directory.Exists(directory)) 
            Directory.CreateDirectory(directory);

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
}
