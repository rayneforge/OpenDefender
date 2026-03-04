using Library.Domain.Models.State;
using Microsoft.AspNetCore.OData;
using ModelContextProtocol.AspNetCore;
using Service;
using Service.Services;
using Library.Application.Producers;
using Library.Application.Consumers;
using Library.Infrastructure.Database;
using Library.Infrastructure.MessageBus;
using Library.Domain.Abstractions;
using Library.Application.Agents.Delegates;
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

// ── LLM-gated registrations ─────────────────────────────────────────────────
// When the "Service:Llm" section is absent from configuration, the service runs
// in "headless" mode: no IChatClient, no agent controllers, and no agent triggers
// in the task scheduler. Producers, consumers, OData, and MCP still work.
var llmEnabled = serviceOptions.Llm is not null;

if (llmEnabled)
{

    // Register IChatClient (LLM) using configured ServiceOptions
    builder.Services.AddSingleton<Microsoft.Extensions.AI.IChatClient>(services =>
    {
        var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServiceOptions>>().Value;
        return Library.Application.Factories.ChatClientFactory.Create(options.Llm!);
    });
    
    // Register agents
    builder.Services.AddTransient<Library.Application.Agents.Delegates.ShieldAgent>();
    builder.Services.AddTransient<Library.Application.Agents.Delegates.AnchorAgent>();
    builder.Services.AddTransient<Library.Application.Agents.Delegates.CoreAgent>();
    builder.Services.AddTransient<Library.Application.Agents.Delegates.LedgerAgent>();
}
if (!llmEnabled)
{
    // Strip agent controllers so MVC never tries to resolve them (they need IChatClient)
    builder.Services.AddControllers()
        .ConfigureApplicationPartManager(manager =>
        {
            manager.FeatureProviders.Add(
                new Service.AgentControllerExclusionProvider());
        });
}

// Register the task channel (singleton — shared between scheduler, dispatcher, and controllers)
var taskChannel = new TaskChannel();
builder.Services.AddSingleton(taskChannel);
builder.Services.AddSingleton<ITaskChannel>(taskChannel);

// Build the task registry at startup — scan the entire Library assembly.
// When LLM is disabled, exclude agent types (BaseAgent subclasses) so their
// [TaskTrigger] methods are never scheduled. Producers & consumers still run.
var assemblyTypes = typeof(DiagnosticProducer).Assembly.GetTypes();
var registryTypes = llmEnabled
    ? assemblyTypes
    : assemblyTypes.Where(t => !t.IsSubclassOf(typeof(Library.Domain.Abstractions.BaseAgent)));

var registryLoggerFactory = LoggerFactory.Create(b =>
{
    if (serviceOptions.TransportType == "Stdio")
        b.AddConsole(opt => opt.LogToStandardErrorThreshold = LogLevel.Trace);
    else
        b.AddConsole();
});
var registry = TaskRegistry.Build(
    registryTypes,
    registryLoggerFactory.CreateLogger("TaskRegistry"));
builder.Services.AddSingleton(registry);

// Two hosted services replace the previous four
builder.Services.AddHostedService<TaskSchedulerService>();
builder.Services.AddHostedService<TaskDispatcherService>();

// If "Stdio", disable HTTP server and setup MCP loop
if (serviceOptions.TransportType == "Stdio")
{
    builder.WebHost.UseKestrel(opts => opts.Listen(System.Net.IPAddress.Loopback, 0));
}

var app = builder.Build();

// Run orchestrators on startup if database is empty.
// In Stdio mode this must not block before app.Run() — the MCP transport
// can only respond to the initialize handshake once the host is running.
if (serviceOptions.TransportType != "Stdio")
{
    using var scope = app.Services.CreateScope();
    var reportDb = scope.ServiceProvider.GetRequiredService<ReportDbContext>();
    var analyticsDb = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Ensuring databases are created...");
    await reportDb.Database.EnsureCreatedAsync();
    await analyticsDb.Database.EnsureCreatedAsync();

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
            var diagnosticOrchestrator = new DiagnosticOrchestrator(since: since);
            await diagnosticOrchestrator.RunAsync(state);

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
else
{
    // Stdio mode: ensure DB exists but defer heavy orchestration to background
    using var scope = app.Services.CreateScope();
    var reportDb = scope.ServiceProvider.GetRequiredService<ReportDbContext>();
    var analyticsDb = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    await reportDb.Database.EnsureCreatedAsync();
    await analyticsDb.Database.EnsureCreatedAsync();
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
