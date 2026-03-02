using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Results;
using Library.Infrastructure.Database; // Covers AnalyticsDbContext
using Library.Domain.Models.Analytics;
using System.Linq;

namespace Service.Controllers;

public class ResourceAnalyticsController : ODataController
{
    private readonly AnalyticsDbContext _db;
    public ResourceAnalyticsController(AnalyticsDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<ResourceAnalytics> Get() => _db.ResourceAnalytics;
    
    [EnableQuery]
    public SingleResult<ResourceAnalytics> Get([FromRoute] int key) => SingleResult.Create(_db.ResourceAnalytics.Where(c => c.Id == key));
}

public class SecurityAnalyticsController : ODataController
{
    private readonly AnalyticsDbContext _db;
    public SecurityAnalyticsController(AnalyticsDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<SecurityAnalytics> Get() => _db.SecurityAnalytics;
    
    [EnableQuery]
    public SingleResult<SecurityAnalytics> Get([FromRoute] int key) => SingleResult.Create(_db.SecurityAnalytics.Where(c => c.Id == key));
}

public class ReliabilityAnalyticsController : ODataController
{
    private readonly AnalyticsDbContext _db;
    public ReliabilityAnalyticsController(AnalyticsDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<ReliabilityAnalytics> Get() => _db.ReliabilityAnalytics;
    
    [EnableQuery]
    public SingleResult<ReliabilityAnalytics> Get([FromRoute] int key) => SingleResult.Create(_db.ReliabilityAnalytics.Where(c => c.Id == key));
}

public class LedgerAnalyticsController : ODataController
{
    private readonly AnalyticsDbContext _db;
    public LedgerAnalyticsController(AnalyticsDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<LedgerAnalytics> Get() => _db.LedgerAnalytics;
    
    [EnableQuery]
    public SingleResult<LedgerAnalytics> Get([FromRoute] int key) => SingleResult.Create(_db.LedgerAnalytics.Where(c => c.Id == key));
}
