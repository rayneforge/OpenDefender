// using Microsoft.Extensions.AI;
// using Library.Application.Factories;
// using Library.Domain.Models.Evaluation;
// using Tests.Utilities;
// using Tests.Utilities.Fixtures;
// using Xunit.Abstractions;

// namespace Tests.Mcp;

// /// <summary>
// /// Uses <see cref="McpHttpFixture"/> pointed at the public Microsoft Learn
// /// MCP endpoint (<c>https://learn.microsoft.com/api/mcp</c>).
// /// Set <c>MCP_HTTP_ENDPOINT=https://learn.microsoft.com/api/mcp</c> to run.
// /// The agent is given every tool the remote server advertises and asked a
// /// documentation question — we verify it picks a search/query tool.
// /// </summary>
// [Collection(McpHttpCollection.Name)]
// public class MsLearnAgentTests : IDisposable
// {
//     private readonly McpHttpFixture _fixture;
//     private readonly IChatClient _agent;
//     private readonly ITestOutputHelper _output;

//     public MsLearnAgentTests(McpHttpFixture fixture, ITestOutputHelper output)
//     {
//         _fixture = fixture;
//         _output = output;
//         _agent = ChatClientFactory.Create();
//     }

//     public void Dispose() => (_agent as IDisposable)?.Dispose();

//     [Fact]
//     public async Task Agent_CallsMsLearnSearchTool()
//     {
//         var tools = _fixture.Tools.ToList<AITool>();

//         var messages = new List<ChatMessage>
//         {
//             new(ChatRole.User,
//                 "Search for documentation on how to use IChatClient with tool calling in .NET.")
//         };

//         var response = await _agent.GetResponseAsync(messages, new() { Tools = tools });
//         ResponseLogger.Log(_output, response);

//         var calls = ResponseAssertions.GetToolCalls(response);
//         Assert.NotEmpty(calls);
//     }

//     [Fact]
//     public async Task Agent_EvaluatedByJudge_ForMicrosoftLearn()
//     {
//         var tools = _fixture.Tools.ToList<AITool>();

//         const string systemPrompt = "You are a specialized documentation assistant for Microsoft Learn.";
//         const string userInput = "How do I implement a custom IChatClient for testing?";

//         var messages = new List<ChatMessage>
//         {
//             new(ChatRole.System, systemPrompt),
//             new(ChatRole.User, userInput)
//         };

//         // 1. Run the agent
//         var response = await _agent.GetResponseAsync(messages, new() { Tools = tools });
//         ResponseLogger.Log(_output, response);

//         // 2. Prepare wrap for evaluation
//         var runResponse = AgentRunResponse<string>.FromText(response.Text, response.Messages);

//         // 3. Harness evaluation using fluent fixture
//         var evalFixture = new McpEvaluationFixture(_agent, _output); // Reuse agent as judge for demo
//         var result = await evalFixture.EvaluateAsync(
//             systemPrompt,
//             userInput,
//             runResponse,
//             "The agent must provide an accurate, useful answer about implementing IChatClient for testing in .NET, including at least one concrete code example or reference to documentation."
//         );

//         evalFixture.Expect(result, r => r.Score.TaskSuccess > 0.5, "Task success score too low.");
//         evalFixture.Expect(result, r => r.Score.Hallucination < 0.2, "Hallucination score too high.");
//     }
// }
