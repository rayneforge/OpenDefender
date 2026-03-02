using ModelContextProtocol.Client;

namespace Tests.Utilities.Fixtures;

/// <summary>
/// Starts the Service over Stdio MCP transport and exposes the connected client.
/// </summary>
public sealed class McpStdioFixture : IAsyncLifetime
{
    private McpClient? _client;
    private IList<McpClientTool>? _tools;

    public McpClient Client => _client ?? throw new InvalidOperationException("Fixture not initialized.");
    public IList<McpClientTool> Tools => _tools ?? throw new InvalidOperationException("Fixture not initialized.");

    private static string ServiceDir =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Service"));

    public async Task InitializeAsync()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "ObservabilityService",
            Command = "dotnet",
            Arguments = ["run", "--project", ServiceDir, "--no-launch-profile"],
        });

        _client = await McpClient.CreateAsync(transport);
        _tools = await _client.ListToolsAsync();
    }

    public async Task DisposeAsync()
    {
        if (_client is IAsyncDisposable d) await d.DisposeAsync();
    }
}

[CollectionDefinition(McpStdioCollection.Name)]
public class McpStdioCollection : ICollectionFixture<McpStdioFixture>
{
    public const string Name = "McpStdio";
}
