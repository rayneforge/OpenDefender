using Microsoft.Extensions.AI;
using ModelContextProtocol;
using Library.Application.Factories;
using Tests.Utilities;
using Tests.Utilities.Fixtures;
using Xunit.Abstractions;

namespace Tests.Mcp;

/// <summary>
/// Proves the agent picks the correct tool from the full InfrastructureMcp domain.
/// The MCP prompt "infrastructure-health-check" is injected as conversation context.
/// </summary>
[Collection(McpStdioCollection.Name)]
public class InfrastructureAgentTests : IDisposable
{
    private const string PromptName = "infrastructure-health-check";

    private static readonly string[] DomainToolNames =
    [
        "query_resource_metrics",
        "query_hardware_metrics",
        "query_kernel_metrics",
        "query_gpu_metrics",
        "query_resource_analytics",
    ];

    private readonly McpStdioFixture _fixture;
    private readonly List<AITool> _tools;
    private readonly IChatClient _agent;
    private readonly ITestOutputHelper _output;

    public InfrastructureAgentTests(McpStdioFixture fixture, ITestOutputHelper output)
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
    public async Task Agent_CallsQueryResourceMetrics()
    {
        var messages = await BuildMessagesAsync(
            "Get the latest resource utilization metrics. Top 5, ordered by Timestamp descending.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_resource_metrics");
    }

    [Fact]
    public async Task Agent_CallsQueryHardwareMetrics()
    {
        var messages = await BuildMessagesAsync(
            "Find hardware components where Status is not OK. Top 5.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_hardware_metrics");
    }

    [Fact]
    public async Task Agent_CallsQueryKernelMetrics()
    {
        var messages = await BuildMessagesAsync(
            "Show me kernel metrics.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_kernel_metrics");
    }

    [Fact]
    public async Task Agent_CallsQueryGpuMetrics()
    {
        var messages = await BuildMessagesAsync(
            "Get GPU temperature metrics ordered by Temp descending. Top 5.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_gpu_metrics");
    }

    [Fact]
    public async Task Agent_CallsQueryResourceAnalytics()
    {
        var messages = await BuildMessagesAsync(
            "Check for resource threshold breaches. Filter where IsBreach equals true.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_resource_analytics");
    }
}
