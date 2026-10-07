using Library.Application.Agents.Delegates;
using Library.Application.Producers;
using Library.Domain.Abstractions;
using Library.Domain.Models.State;
using Library.Infrastructure.Database;
using Library.Infrastructure.MessageBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using Service.Services;

// Resolve bundled configuration independently of the MCP client's working directory.
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? "Production";
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();
var serviceOptions = configuration.GetSection(ServiceOptions.SectionName).Get<ServiceOptions>() ?? new();
if (!string.Equals(serviceOptions.TransportType, "Stdio", StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("OpenDefender supports only Stdio. HTTP transport has been removed.");

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args, ContentRootPath = AppContext.BaseDirectory, EnvironmentName = environment
});
builder.Configuration.Sources.Clear();
builder.Configuration.AddConfiguration(configuration);
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection(ServiceOptions.SectionName));

// stdout belongs exclusively to the MCP transport.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly().WithPromptsFromAssembly();

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

using IHost host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ReportDbContext>().Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>().Database.EnsureCreatedAsync();
}
// The scheduler starts collection without delaying the MCP initialize handshake.
await host.RunAsync();
