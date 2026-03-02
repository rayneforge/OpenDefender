# MCP + Microsoft.Extensions.AI Testing

This document shows a clean xUnit setup that supports:

- MCP via STDIO (local process)
- MCP via HTTP (Streamable HTTP)
- Microsoft.Extensions.AI agent loop (`FunctionInvokingChatClient`)
- MCP prompt injection into agent conversations
- Deterministic tool assertions

> There is no separate "Microsoft Agent Framework (.NET)." The agent loop is `FunctionInvokingChatClient` middleware applied via `.UseFunctionInvocation()` from the `Microsoft.Extensions.AI` package.

---

## 1. Dependencies

```xml
<PackageReference Include="Microsoft.Extensions.AI" Version="10.3.0" />
<PackageReference Include="ModelContextProtocol" Version="1.0.0" />
<PackageReference Include="OllamaSharp" Version="5.4.18" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
```

> `ModelContextProtocol` 1.0.0 no longer has `IMcpClient` or `McpClientFactory`. Use `McpClient.CreateAsync()` which returns a concrete `McpClient`.

---

## 2. Starting MCP (STDIO Mode)

Create an MCP client over STDIO connecting to a .NET MCP server:

```csharp
using ModelContextProtocol;
using ModelContextProtocol.Client;

var transport = new StdioClientTransport(new()
{
    Name = "observability-mcp",
    Command = "dotnet",
    Arguments = ["run", "--project", "path/to/Service.csproj"],
    EnvironmentVariables = new Dictionary<string, string>
    {
        ["MCP_TRANSPORT"] = "Stdio"
    }
});

McpClient client = await McpClient.CreateAsync(transport);
IList<McpClientTool> tools = await client.ListToolsAsync();
```

> `McpClientTool` inherits from `AIFunction` (which inherits from `AITool`), so tools are directly usable with `IChatClient`.

---

## 3. Starting MCP (HTTP Mode)

If MCP exposes an HTTP endpoint:

```csharp
using ModelContextProtocol;
using ModelContextProtocol.Client;

var transport = new HttpClientTransport(new()
{
    Endpoint = new Uri("http://localhost:5000/mcp")
});

McpClient client = await McpClient.CreateAsync(transport);
IList<McpClientTool> tools = await client.ListToolsAsync();
```

---

## 4. Agent Setup

The agent is an `IChatClient` with `FunctionInvokingChatClient` middleware. This middleware intercepts tool-call requests from the model, executes each `AIFunction`, feeds results back, and loops until a final text response.

```csharp
using Microsoft.Extensions.AI;
using OllamaSharp;

IChatClient agent = new ChatClientBuilder(
        new OllamaApiClient(new Uri("http://localhost:11434"), "gpt-oss:120b"))
    .UseFunctionInvocation()
    .Build();
```

Any `IChatClient` implementation works as the inner client (Azure OpenAI, Ollama, etc.). The `.UseFunctionInvocation()` call wraps it with the agent loop.

---

## 5. MCP Prompt Injection

Fetch prompts from the MCP server and prepend them to the conversation before the user query. This gives the agent domain context.

```csharp
using Microsoft.Extensions.AI;

var promptResult = await mcpClient.GetPromptAsync("security-posture-assessment");
IList<ChatMessage> messages = promptResult.ToChatMessages();
messages.Add(new ChatMessage(ChatRole.User, "Which endpoints have the most auth failures?"));
```

> `GetPromptResult.ToChatMessages()` returns `IList<ChatMessage>` (not `List<ChatMessage>`).

---

## 6. Running the Agent

Pass messages and tools to `GetResponseAsync`:

```csharp
var options = new ChatOptions
{
    Tools = tools.Where(t => domainToolNames.Contains(t.Name))
                 .Cast<AITool>()
                 .ToList()
};

ChatResponse response = await agent.GetResponseAsync(messages, options);
```

> Filter tools by domain (e.g., all security tools) rather than a single tool. This gives the model real selection noise to prove it picks the right tool.

---

## 7. Deterministic Tool Assertions

Inspect `ChatResponse.Messages` for `FunctionCallContent` and `FunctionResultContent`:

```csharp
using Microsoft.Extensions.AI;

public static class ResponseAssertions
{
    public static List<FunctionCallContent> GetToolCalls(ChatResponse response) =>
        response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionCallContent>()
            .ToList();

    public static List<FunctionResultContent> GetToolResults(ChatResponse response) =>
        response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionResultContent>()
            .ToList();

    public static List<string> GetCalledToolNames(ChatResponse response) =>
        GetToolCalls(response).Select(c => c.Name).ToList();

    public static void AssertToolWasCalled(ChatResponse response, string toolName) =>
        Assert.Contains(toolName, GetCalledToolNames(response));

    public static void AssertToolHasResult(ChatResponse response, string toolName)
    {
        var results = GetToolResults(response);
        Assert.Contains(results, r => r.CallId != null &&
            GetToolCalls(response).Any(c => c.CallId == r.CallId && c.Name == toolName));
    }
}
```

---

## 8. Full xUnit Test Example

```csharp
public class SecurityAgentTests : IClassFixture<McpStdioFixture>
{
    private static readonly string[] DomainToolNames =
        ["QueryAuthEvents", "QueryRateLimitEvents", "QueryScopeUsage"];

    private const string PromptName = "security-posture-assessment";

    private readonly McpStdioFixture _fixture;
    private readonly IChatClient _agent;
    private readonly IList<AITool> _tools;

    public SecurityAgentTests(McpStdioFixture fixture)
    {
        _fixture = fixture;
        _agent = ChatClientFactory.Create();
        _tools = fixture.Tools
            .Where(t => DomainToolNames.Contains(t.Name))
            .Cast<AITool>()
            .ToList();
    }

    private async Task<IList<ChatMessage>> BuildMessagesAsync(string userInput)
    {
        var promptResult = await _fixture.Client.GetPromptAsync(PromptName);
        IList<ChatMessage> messages = promptResult.ToChatMessages();
        messages.Add(new ChatMessage(ChatRole.User, userInput));
        return messages;
    }

    [Fact]
    public async Task Agent_Calls_QueryAuthEvents()
    {
        var messages = await BuildMessagesAsync(
            "Which endpoints have the most authentication failures?");

        var response = await _agent.GetResponseAsync(
            messages, new ChatOptions { Tools = _tools });

        ResponseAssertions.AssertToolWasCalled(response, "QueryAuthEvents");
    }
}
```

---

## 9. AI Foundry Evaluators (Optional)

The `Microsoft.Extensions.AI.Evaluation` library provides semantic evaluators designed for Azure OpenAI deployments.

Built-in evaluators include:

- `ToolCallAccuracyEvaluator`
- `ToolInputAccuracyEvaluator`
- `ToolOutputUtilizationEvaluator`
- `IntentResolutionEvaluator`
- `TaskAdherenceEvaluator`
- `RelevanceEvaluator`
- `GroundednessEvaluator`
- `CoherenceEvaluator`
- `SafetyEvaluator`

These require `AzureOpenAIModelConfiguration` and are independent from the deterministic assertions above.

---

## 10. Recommended Testing Strategy

**CI:**
- STDIO mode
- Deterministic tool assertions (`AssertToolWasCalled`)
- Domain-level tool filtering (all tools from a domain, not just one)
- MCP prompt injection

**Integration:**
- HTTP mode
- Add AI Foundry evaluators if Azure OpenAI is available
- No mocks

