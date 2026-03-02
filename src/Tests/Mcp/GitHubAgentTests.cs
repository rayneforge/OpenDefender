// using Microsoft.Extensions.AI;
// using Library.Application.Factories;
// using Tests.Utilities;
// using Tests.Utilities.Fixtures;
// using Xunit.Abstractions;

// namespace Tests.Mcp;

// /// <summary>
// /// Uses <see cref="McpHttpFixture"/> pointed at the GitHub MCP endpoint.
// /// <para>
// /// Run with a PAT:
// /// <code>
// /// MCP_HTTP_ENDPOINT=https://api.githubcopilot.com/mcp/ \
// /// MCP_AUTH_TYPE=apikey \
// /// MCP_API_KEY=ghp_xxx \
// /// dotnet test --filter GitHubAgentTests
// /// </code>
// /// </para>
// /// <para>
// /// Or with interactive OAuth (dynamic registration + browser):
// /// <code>
// /// MCP_HTTP_ENDPOINT=https://api.githubcopilot.com/mcp/ \
// /// MCP_AUTH_TYPE=oauth \
// /// dotnet test --filter GitHubAgentTests
// /// </code>
// /// </para>
// /// </summary>
// [Collection(McpHttpCollection.Name)]
// public class GitHubAgentTests : IDisposable
// {
//     private readonly McpHttpFixture _fixture;
//     private readonly IChatClient _agent;
//     private readonly ITestOutputHelper _output;

//     public GitHubAgentTests(McpHttpFixture fixture, ITestOutputHelper output)
//     {
//         _fixture = fixture;
//         _output = output;
//         _agent = ChatClientFactory.Create();
//     }

//     public void Dispose() => (_agent as IDisposable)?.Dispose();

//     [Fact]
//     public async Task Agent_ListsRepositories()
//     {
//         var tools = _fixture.Tools.ToList<AITool>();

//         var messages = new List<ChatMessage>
//         {
//             new(ChatRole.User,
//                 "List my GitHub repositories. Just show the first few names.")
//         };

//         var response = await _agent.GetResponseAsync(messages, new() { Tools = tools });
//         ResponseLogger.Log(_output, response);

//         var calls = ResponseAssertions.GetCalledToolNames(response);
//         Assert.NotEmpty(calls);
//         // The agent should use a repo listing tool
//         Assert.Contains(calls, name => name.Contains("repo", StringComparison.OrdinalIgnoreCase)
//                                      || name.Contains("list", StringComparison.OrdinalIgnoreCase));
//     }
// }
