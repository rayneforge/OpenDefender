using Microsoft.Extensions.AI;
using ModelContextProtocol;
using Library.Application.Factories;
using Tests.Utilities;
using Tests.Utilities.Fixtures;
using Xunit.Abstractions;

namespace Tests.Mcp;

/// <summary>
/// Proves the agent picks the correct tool from the full OrchestrationMcp domain.
/// The MCP prompt "orchestration-run-review" is injected as conversation context.
/// </summary>
[Collection(McpStdioCollection.Name)]
public class OrchestrationAgentTests : IDisposable
{
    private const string PromptName = "orchestration-run-review";

    private static readonly string[] DomainToolNames =
    [
        "query_orchestrations",
    ];

    private readonly McpStdioFixture _fixture;
    private readonly List<AITool> _tools;
    private readonly IChatClient _agent;
    private readonly ITestOutputHelper _output;

    public OrchestrationAgentTests(McpStdioFixture fixture, ITestOutputHelper output)
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
    public async Task Agent_CallsQueryOrchestrations()
    {
        var messages = await BuildMessagesAsync(
            "Get the 5 most recent orchestration runs ordered by StartTime descending.");
        var response = await _agent.GetResponseAsync(messages, new() { Tools = _tools });
        ResponseLogger.Log(_output, response);

        ResponseAssertions.AssertToolWasCalled(response, "query_orchestrations");
    }
}
