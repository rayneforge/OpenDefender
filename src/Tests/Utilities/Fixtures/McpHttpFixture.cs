using ModelContextProtocol.Client;

namespace Tests.Utilities.Fixtures;

/// <summary>
/// Connects an MCP client to a running Service instance over HTTP.
/// The Service must already be running in HTTP mode.
/// Default endpoint: http://localhost:5000/mcp (override via OBSERVABILITY_MCP_ENDPOINT env var).
/// </summary>
public sealed class McpHttpFixture : IAsyncLifetime
{
    private McpClient? _client;
    private IList<McpClientTool>? _tools;

    public McpClient Client => _client ?? throw new InvalidOperationException("Fixture not initialized.");
    public IList<McpClientTool> Tools => _tools ?? throw new InvalidOperationException("Fixture not initialized.");

    public async Task InitializeAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("OBSERVABILITY_MCP_ENDPOINT")
            ?? "http://localhost:5000/mcp";

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Name = "ObservabilityService-Http",
            Endpoint = new Uri(endpoint),
        });

        _client = await McpClient.CreateAsync(transport);
        _tools = await _client.ListToolsAsync();
    }

    public async Task DisposeAsync()
    {
        if (_client is IAsyncDisposable d) await d.DisposeAsync();
    }
}

[CollectionDefinition(McpHttpCollection.Name)]
public class McpHttpCollection : ICollectionFixture<McpHttpFixture>
{
    public const string Name = "McpHttp";
}
