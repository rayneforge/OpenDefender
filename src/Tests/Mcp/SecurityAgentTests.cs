using Microsoft.Extensions.AI;
using ModelContextProtocol;
using Tests.Utilities;
using Tests.Utilities.Fixtures;
using Xunit.Abstractions;

namespace Tests.Mcp;

/// <summary>
/// Proves the agent picks the correct tool from the full SecurityMcp domain.
/// The MCP prompt "security-posture-assessment" is injected as conversation
/// context so the agent has domain strategy before seeing the user query.
/// </summary>
[Collection(McpStdioCollection.Name)]
public class SecurityAgentTests : IDisposable
{
    private const string PromptName = "security-posture-assessment";

    private static readonly string[] DomainToolNames =
    [
        "query_security_checks",
        "query_networking_metrics",
        "query_packet_tracing",
        "query_security_analytics",
    ];

    private readonly McpStdioFixture _fixture;
    private readonly List<AITool> _tools;
    private readonly IChatClient _agent;
    private readonly ITestOutputHelper _output;

    public SecurityAgentTests(McpStdioFixture fixture, ITestOutputHelper output)
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
    public async Task Agent_CallsQuerySecurityChecks()
    {
        var messages = await BuildMessagesAsync(
            "Get the 3 most recent security checks ordered by Timestamp descending.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_security_checks");
    }

    [Fact]
    public async Task Agent_CallsQueryNetworkingMetrics()
    {
        var messages = await BuildMessagesAsync(
            "Show me the latest networking metrics.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_networking_metrics");
    }

    [Fact]
    public async Task Agent_CallsQueryPacketTracing()
    {
        var messages = await BuildMessagesAsync(
            "Retrieve packet tracing data.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_packet_tracing");
    }

    [Fact]
    public async Task Agent_CallsQuerySecurityAnalytics()
    {
        var messages = await BuildMessagesAsync(
            "Check for security breaches. Filter where IsBreach equals true, top 5.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_security_analytics");
    }
}
