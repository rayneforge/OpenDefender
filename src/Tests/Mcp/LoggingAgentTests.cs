using Microsoft.Extensions.AI;
using ModelContextProtocol;
using Library.Application.Factories;
using Tests.Utilities;
using Tests.Utilities.Fixtures;
using Xunit.Abstractions;

namespace Tests.Mcp;

/// <summary>
/// Proves the agent picks the correct tool from the full LoggingMcp domain.
/// The MCP prompt "logging-retention-audit" is injected as conversation context.
/// </summary>
[Collection(McpStdioCollection.Name)]
public class LoggingAgentTests : IDisposable
{
    private const string PromptName = "logging-retention-audit";

    private static readonly string[] DomainToolNames =
    [
        "query_logging_metrics",
        "query_logging_inventory",
        "query_ledger_analytics",
    ];

    private readonly McpStdioFixture _fixture;
    private readonly List<AITool> _tools;
    private readonly IChatClient _agent;
    private readonly ITestOutputHelper _output;

    public LoggingAgentTests(McpStdioFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _tools = fixture.Tools.Where(t => DomainToolNames.Contains(t.Name)).ToList<AITool>();
        _agent = ChatClientFactory.Create();
    }

    public void Dispose() => (_agent as IDisposable)?.Dispose();

    private async Task<IList<ChatMessage>> BuildMessagesAsync(string userInput)
    {
        var promptResult = await _fixture.Client.GetPromptAsync(PromptName);
        var messages = promptResult.ToChatMessages();
        messages.Add(new ChatMessage(ChatRole.User, userInput));
        return messages;
    }

    [Fact]
    public async Task Agent_CallsQueryLoggingMetrics()
    {
        var messages = await BuildMessagesAsync(
            "Get logging pipeline throughput metrics.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_logging_metrics");
    }

    [Fact]
    public async Task Agent_CallsQueryLoggingInventory()
    {
        var messages = await BuildMessagesAsync(
            "Show me log source inventory ordered by SizeBytes descending. Top 5.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_logging_inventory");
    }

    [Fact]
    public async Task Agent_CallsQueryLedgerAnalytics()
    {
        var messages = await BuildMessagesAsync(
            "Find non-compliant sources. Filter where IsRetentionCompliant equals false.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_ledger_analytics");
    }
}
