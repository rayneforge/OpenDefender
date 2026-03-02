using Library.Domain.Models.State;
using Microsoft.AspNetCore.OData;
using ModelContextProtocol.AspNetCore;
using Service;
using Service.Services;
using Library.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Library.Application.Services.Orchestration;

var builder = WebApplication.CreateBuilder(args);

// Configure Diagnostic Options from appsettings.json
builder.Services.Configure<ServiceOptions>(
    builder.Configuration.GetSection(ServiceOptions.SectionName));

// Resolve options for early access
var serviceOptions = builder.Configuration.GetSection(ServiceOptions.SectionName).Get<ServiceOptions>() ?? new ServiceOptions();

// If Stdio, ensure logs don't corrupt stdout (the protocol channel)
if (serviceOptions.TransportType == "Stdio")
{
    builder.Logging.ClearProviders();
    // Redirect logs to debug/stderr or file if needed, but keep stdout clean
    builder.Logging.AddConsole(opt => opt.LogToStandardErrorThreshold = LogLevel.Trace); 
}

// Register MCP Server — tools auto-discovered from assembly
if (serviceOptions.TransportType == "Stdio")
{
    builder.Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly()
        .WithPromptsFromAssembly();
}
else
{
    builder.Services.AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly()
        .WithPromptsFromAssembly();
}

// Register ReportDbContext
builder.Services.AddDbContext<Library.Infrastructure.Database.ReportDbContext>();
builder.Services.AddDbContext<Library.Infrastructure.Database.AnalyticsDbContext>();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddOData(opt => opt
        .Count().Filter().Expand().Select().OrderBy().SetMaxTop(100)
        .AddRouteComponents("odata/metrics", EdmModelBuilder.GetReportModel())
        .AddRouteComponents("odata/analytics", EdmModelBuilder.GetAnalyticsModel())
    );

// Register the Orchestrator as a background worker
builder.Services.AddHostedService<DiagnosticHostedService>();
builder.Services.AddHostedService<AnalyticsHostedService>();
builder.Services.AddHostedService<RetentionHostedService>();

// If "Stdio", disable HTTP server and setup MCP loop
if (serviceOptions.TransportType == "Stdio")
{
    builder.WebHost.UseKestrel(opts => opts.Listen(System.Net.IPAddress.Loopback, 0));
}

var app = builder.Build();

// Run orchestrators on startup if database is empty
using (var scope = app.Services.CreateScope())
{
    var reportDb = scope.ServiceProvider.GetRequiredService<ReportDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (!await reportDb.Orchestrations.AnyAsync())
    {
        logger.LogInformation("Database is empty. Running initial orchestration...");
        
        var state = new OrchestrationState
        {
            RunId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };

        var since = DateTime.UtcNow.AddHours(-serviceOptions.LookbackHours);
        
        try 
        {
            // 1. Collect raw metrics
            var diagnosticOrchestrator = new DiagnosticOrchestrator(since: since);
            await diagnosticOrchestrator.RunAsync(state);

            // 2. Generate derived analytics
            var analyticsOrchestrator = new AnalyticsOrchestrator();
            await analyticsOrchestrator.RunAsync(state);

            logger.LogInformation("Initial orchestration complete. RunId: {Id}", state.RunId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during initial orchestration run on empty database.");
        }
    }
}

// Configure the HTTP request pipeline.
if (serviceOptions.TransportType == "Http")
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.MapControllers();
    app.MapMcp();
}
else if (serviceOptions.TransportType == "Stdio")
{
    // Stdio MCP transport is handled by the hosted service registered above.
    // No HTTP pipeline needed.
}

app.Run();
