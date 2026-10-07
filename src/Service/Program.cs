using Library.Application.Agents.Delegates;
using Library.Application.Producers;
using Library.Domain.Abstractions;
using Library.Domain.Models.State;
using Library.Infrastructure.Database;
using Library.Infrastructure.MessageBus;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.AspNetCore;
using Service;
using Service.Services;

// Resolve bundled configuration independently of the MCP client's working directory.
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();
var serviceOptions = configuration.GetSection(ServiceOptions.SectionName).Get<ServiceOptions>() ?? new();
var stdio = string.Equals(serviceOptions.TransportType, "Stdio", StringComparison.OrdinalIgnoreCase);
if (!stdio && !string.Equals(serviceOptions.TransportType, "Http", StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("Service:TransportType must be Stdio or Http.");

WebApplicationBuilder? webBuilder = stdio ? null : WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args, ContentRootPath = AppContext.BaseDirectory, EnvironmentName = environment
});
IHostApplicationBuilder builder = webBuilder is not null ? webBuilder : Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args, ContentRootPath = AppContext.BaseDirectory, EnvironmentName = environment
});
builder.Configuration.Sources.Clear();
builder.Configuration.AddConfiguration(configuration);
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection(ServiceOptions.SectionName));

// stdout belongs exclusively to the MCP transport.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
var mcp = builder.Services.AddMcpServer().WithToolsFromAssembly().WithPromptsFromAssembly();
if (stdio) mcp.WithStdioServerTransport();
else mcp.WithHttpTransport();

builder.Services.AddDbContext<ReportDbContext>();
builder.Services.AddDbContext<AnalyticsDbContext>();
var llmEnabled = serviceOptions.Llm is not null;
if (llmEnabled)
{
    builder.Services.AddSingleton<Microsoft.Extensions.AI.IChatClient>(services =>
    {
        var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServiceOptions>>().Value;
        return Library.Application.Factories.ChatClientFactory.Create(options.Llm!);
    });
    builder.Services.AddTransient<ShieldAgent>();
    builder.Services.AddTransient<AnchorAgent>();
    builder.Services.AddTransient<CoreAgent>();
    builder.Services.AddTransient<LedgerAgent>();
}

if (webBuilder is not null)
{
    builder.Services.AddOpenApi();
    var controllers = builder.Services.AddControllers().AddOData(options => options
        .Count().Filter().Expand().Select().OrderBy().SetMaxTop(100)
        .AddRouteComponents("odata/metrics", EdmModelBuilder.GetReportModel())
        .AddRouteComponents("odata/analytics", EdmModelBuilder.GetAnalyticsModel()));
    if (!llmEnabled)
        controllers.ConfigureApplicationPartManager(manager =>
            manager.FeatureProviders.Add(new AgentControllerExclusionProvider()));
}

var channel = new TaskChannel();
builder.Services.AddSingleton(channel);
builder.Services.AddSingleton<ITaskChannel>(channel);
var types = typeof(DiagnosticProducer).Assembly.GetTypes();
using var registryLoggerFactory = LoggerFactory.Create(logging =>
    logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace));
builder.Services.AddSingleton(TaskRegistry.Build(
    llmEnabled ? types : types.Where(type => !type.IsSubclassOf(typeof(BaseAgent))),
    registryLoggerFactory.CreateLogger("TaskRegistry")));
builder.Services.AddHostedService<TaskSchedulerService>();
builder.Services.AddHostedService<TaskDispatcherService>();

using IHost host = webBuilder is not null ? webBuilder.Build() : ((HostApplicationBuilder)builder).Build();
using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ReportDbContext>().Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>().Database.EnsureCreatedAsync();
}
// The scheduler starts collection without delaying the MCP initialize handshake.
if (host is WebApplication app)
{
    if (app.Environment.IsDevelopment()) app.MapOpenApi();
    app.UseHttpsRedirection();
    app.MapControllers();
    app.MapMcp("/mcp");
}
await host.RunAsync();
