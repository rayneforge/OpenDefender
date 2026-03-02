using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.OData.Results;
using Library.Infrastructure.Database;
using Library.Domain.Models.Metrics;
using Library.Domain.Models.State;

namespace Service.Controllers;

public class OrchestrationsController : ODataController
{
    private readonly ReportDbContext _db;
    public OrchestrationsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<OrchestrationState> Get() => _db.Orchestrations;
    
    [EnableQuery]
    public SingleResult<OrchestrationState> Get([FromRoute] int key) => SingleResult.Create(_db.Orchestrations.Where(c => c.Id == key));
}

public class AutomationMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public AutomationMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<AutomationMetric> Get() => _db.AutomationMetrics;
    
    [EnableQuery]
    public SingleResult<AutomationMetric> Get([FromRoute] int key) => SingleResult.Create(_db.AutomationMetrics.Where(c => c.Id == key));
}

public class ControlMapMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public ControlMapMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<ControlMapMetric> Get() => _db.ControlMapMetrics;
    
    [EnableQuery]
    public SingleResult<ControlMapMetric> Get([FromRoute] int key) => SingleResult.Create(_db.ControlMapMetrics.Where(c => c.Id == key));
}

public class DataRecoveryMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public DataRecoveryMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<DataRecoveryMetric> Get() => _db.DataRecoveryMetrics;
     [EnableQuery]
    public SingleResult<DataRecoveryMetric> Get([FromRoute] int key) => SingleResult.Create(_db.DataRecoveryMetrics.Where(c => c.Id == key));
}

public class GpuMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public GpuMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<GpuMetric> Get() => _db.GpuMetrics;
     [EnableQuery]
    public SingleResult<GpuMetric> Get([FromRoute] int key) => SingleResult.Create(_db.GpuMetrics.Where(c => c.Id == key));
}

public class HardwareMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public HardwareMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<HardwareMetric> Get() => _db.HardwareMetrics;
     [EnableQuery]
    public SingleResult<HardwareMetric> Get([FromRoute] int key) => SingleResult.Create(_db.HardwareMetrics.Where(c => c.Id == key));
}

public class KernelMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public KernelMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<KernelMetric> Get() => _db.KernelMetrics;
     [EnableQuery]
    public SingleResult<KernelMetric> Get([FromRoute] int key) => SingleResult.Create(_db.KernelMetrics.Where(c => c.Id == key));
}

public class LoggingMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public LoggingMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<LoggingMetric> Get() => _db.LoggingMetrics;
     [EnableQuery]
    public SingleResult<LoggingMetric> Get([FromRoute] int key) => SingleResult.Create(_db.LoggingMetrics.Where(c => c.Id == key));
}

public class LoggingInventoryMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public LoggingInventoryMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<LoggingInventoryMetric> Get() => _db.LoggingInventoryMetrics;
     [EnableQuery]
    public SingleResult<LoggingInventoryMetric> Get([FromRoute] int key) => SingleResult.Create(_db.LoggingInventoryMetrics.Where(c => c.Id == key));
}

public class NetworkingMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public NetworkingMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<NetworkingMetric> Get() => _db.NetworkingMetrics;
     [EnableQuery]
    public SingleResult<NetworkingMetric> Get([FromRoute] int key) => SingleResult.Create(_db.NetworkingMetrics.Where(c => c.Id == key));
}

public class PacketTracingMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public PacketTracingMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<PacketTracingMetric> Get() => _db.PacketTracingMetrics;
     [EnableQuery]
    public SingleResult<PacketTracingMetric> Get([FromRoute] int key) => SingleResult.Create(_db.PacketTracingMetrics.Where(c => c.Id == key));
}

public class ResourceMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public ResourceMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<ResourceMetric> Get() => _db.ResourceMetrics;
     [EnableQuery]
    public SingleResult<ResourceMetric> Get([FromRoute] int key) => SingleResult.Create(_db.ResourceMetrics.Where(c => c.Id == key));
}

public class SecurityChecksController : ODataController
{
    private readonly ReportDbContext _db;
    public SecurityChecksController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<SecurityCheck> Get() => _db.SecurityChecks;
     [EnableQuery]
    public SingleResult<SecurityCheck> Get([FromRoute] int key) => SingleResult.Create(_db.SecurityChecks.Where(c => c.Id == key));
}

public class ServiceMetricsController : ODataController
{
    private readonly ReportDbContext _db;
    public ServiceMetricsController(ReportDbContext db) => _db = db;

    [EnableQuery]
    public IQueryable<ServiceMetric> Get() => _db.ServiceMetrics;
     [EnableQuery]
    public SingleResult<ServiceMetric> Get([FromRoute] int key) => SingleResult.Create(_db.ServiceMetrics.Where(c => c.Id == key));
}
