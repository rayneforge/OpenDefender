using Microsoft.Extensions.AI;
using ModelContextProtocol;
using Library.Application.Factories;
using Tests.Utilities;
using Tests.Utilities.Fixtures;
using Xunit.Abstractions;

namespace Tests.Mcp;

/// <summary>
/// Proves the agent picks the correct tool from the full ReliabilityMcp domain.
/// The MCP prompt "reliability-stability-review" is injected as conversation context.
/// </summary>
[Collection(McpStdioCollection.Name)]
public class ReliabilityAgentTests : IDisposable
{
    private const string PromptName = "reliability-stability-review";

    private static readonly string[] DomainToolNames =
    [
        "query_data_recovery",
        "query_service_metrics",
        "query_control_map",
        "query_automation_metrics",
        "query_reliability_analytics",
    ];

    private readonly McpStdioFixture _fixture;
    private readonly List<AITool> _tools;
    private readonly IChatClient _agent;
    private readonly ITestOutputHelper _output;

    public ReliabilityAgentTests(McpStdioFixture fixture, ITestOutputHelper output)
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
    public async Task Agent_CallsQueryDataRecovery()
    {
        var messages = await BuildMessagesAsync(
            "Get backup metrics ordered by SizeBytes descending, top 3.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_data_recovery");
    }

    [Fact]
    public async Task Agent_CallsQueryServiceMetrics()
    {
        var messages = await BuildMessagesAsync(
            "Find services where Status is not OK.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_service_metrics");
    }

    [Fact]
    public async Task Agent_CallsQueryControlMap()
    {
        var messages = await BuildMessagesAsync(
            "Show me the control map entries.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_control_map");
    }

    [Fact]
    public async Task Agent_CallsQueryAutomationMetrics()
    {
        var messages = await BuildMessagesAsync(
            "Get automation task metrics.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_automation_metrics");
    }

    [Fact]
    public async Task Agent_CallsQueryReliabilityAnalytics()
    {
        var messages = await BuildMessagesAsync(
            "Check for degraded systems. Filter where IsDegraded equals true.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_reliability_analytics");
    }
}
