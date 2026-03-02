# MCP + Microsoft.Extensions.AI Testing

This document shows a clean xUnit setup that supports:

- MCP via STDIO (local process)
- MCP via HTTP (Streamable HTTP — local or remote endpoints)
- Microsoft.Extensions.AI agent loop (`FunctionInvokingChatClient`)
- MCP prompt injection into agent conversations
- Deterministic tool assertions
- Structured response logging

> There is no separate "Microsoft Agent Framework (.NET)." The agent loop is `FunctionInvokingChatClient` middleware applied via `.UseFunctionInvocation()` from the `Microsoft.Extensions.AI` package.

---

## 1. Dependencies

```xml
<PackageReference Include="coverlet.collector" Version="6.0.4" />
<PackageReference Include="Microsoft.Extensions.AI" Version="10.3.0" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
<PackageReference Include="ModelContextProtocol" Version="1.0.0" />
<PackageReference Include="OllamaSharp" Version="5.4.18" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
```

The Tests project also references the Library project (which contains `ChatClientFactory`):

```xml
<ProjectReference Include="..\Library\Library.csproj" />
```

> `ModelContextProtocol` 1.0.0 no longer has `IMcpClient` or `McpClientFactory`. Use `McpClient.CreateAsync()` which returns a concrete `McpClient`.

---

## 2. Starting MCP (STDIO Mode)

Create an MCP client over STDIO connecting to a .NET MCP server:

```csharp
using ModelContextProtocol.Client;

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Name = "ObservabilityService",
    Command = "dotnet",
    Arguments = ["run", "--project", serviceDir, "--no-launch-profile"],
});

McpClient client = await McpClient.CreateAsync(transport);
IList<McpClientTool> tools = await client.ListToolsAsync();
```

> `McpClientTool` inherits from `AIFunction` (which inherits from `AITool`), so tools are directly usable with `IChatClient`.

The STDIO fixture is shared across test classes via a **collection fixture**:

```csharp
[Collection(McpStdioCollection.Name)]
public class SecurityAgentTests { ... }
```

This ensures a single MCP server process is reused for all tests in the collection.

---

## 3. Starting MCP (HTTP Mode)

The HTTP fixture connects to any Streamable HTTP MCP endpoint with pluggable authentication. All configuration is via environment variables.

### Transport

| Variable | Default | Description |
|---|---|---|
| `MCP_HTTP_ENDPOINT` | `http://localhost:5000/mcp` | The MCP server URL |
| `MCP_HTTP_NAME` | Derived from host | A friendly name for the transport |

### Authentication (MCP_AUTH_TYPE)

| Value | Description | Additional Variables |
|---|---|---|
| `none` | (default) No authentication | — |
| `apikey` | Sends a key as a Bearer token or custom header | `MCP_API_KEY` (required), `MCP_API_KEY_HEADER` (optional, e.g. `x-api-key`) |
| `oauth` | Auto-probe OAuth. Probes the endpoint’s discovery chain (RFC 9728 → RFC 8414) on startup. If the AS advertises a `registration_endpoint`, uses dynamic registration (no client ID needed). Otherwise requires `OAUTH_CLIENT_ID`. Authorization Code + PKCE via loopback browser redirect. | `OAUTH_CLIENT_ID` (required if no dynamic reg), `OAUTH_CLIENT_SECRET`, `OAUTH_SCOPES`, `OAUTH_REDIRECT_URI` |
| `entra_default` | Azure `DefaultAzureCredential` → Bearer token | `MCP_ENTRA_SCOPE` (required), `MCP_ENTRA_TENANT_ID` (optional) |

Auth is implemented using built-in SDK features — no custom abstractions:

- **API key** → `HttpClientTransportOptions.AdditionalHeaders`
- **OAuth** → `HttpClientTransportOptions.OAuth` (`ClientOAuthOptions` with PKCE, optional dynamic registration, loopback browser redirect, token caching)
- **Entra** → `DefaultAzureCredential.GetTokenAsync()` → Bearer token via `AdditionalHeaders` (chains through env vars, managed identity, VS, Azure CLI, etc.)

```csharp
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;

var endpoint = new Uri(
    Environment.GetEnvironmentVariable("MCP_HTTP_ENDPOINT")
    ?? "http://localhost:5000/mcp");

var options = new HttpClientTransportOptions
{
    Name = endpoint.Host,
    Endpoint = endpoint,
};

// API key auth
var key = Environment.GetEnvironmentVariable("MCP_API_KEY");
if (key is not null)
{
    options.AdditionalHeaders = new Dictionary<string, string>
    {
        ["Authorization"] = $"Bearer {key}"
    };
}

// OAuth auth
var clientId = Environment.GetEnvironmentVariable("OAUTH_CLIENT_ID");
if (clientId is not null)
{
    options.OAuth = new ClientOAuthOptions
    {
        RedirectUri = new Uri("http://localhost:8900/"),
        ClientId = clientId,
    };
}

var transport = new HttpClientTransport(options);
McpClient client = await McpClient.CreateAsync(transport);
IList<McpClientTool> tools = await client.ListToolsAsync();
```

Examples:

```bash
# No auth — local service
dotnet test --filter MsLearnAgentTests

# No auth — remote public endpoint
MCP_HTTP_ENDPOINT=https://learn.microsoft.com/api/mcp dotnet test --filter MsLearnAgentTests

# API key
MCP_HTTP_ENDPOINT=https://example.com/mcp MCP_AUTH_TYPE=apikey MCP_API_KEY=sk-xxx dotnet test

# Custom header
MCP_AUTH_TYPE=apikey MCP_API_KEY=mykey MCP_API_KEY_HEADER=x-api-key dotnet test

# OAuth (interactive browser — dynamic registration)
MCP_AUTH_TYPE=oauth OAUTH_SCOPES="openid profile" dotnet test

# OAuth (pre-registered client — e.g. GitHub)
MCP_HTTP_ENDPOINT=https://api.githubcopilot.com/mcp/ MCP_AUTH_TYPE=oauth OAUTH_CLIENT_ID=Iv1_abc123 dotnet test

# Entra ID (DefaultAzureCredential — uses az login, managed identity, etc.)
MCP_AUTH_TYPE=entra_default MCP_ENTRA_SCOPE=api://my-app/.default dotnet test

# Entra ID with explicit tenant
MCP_AUTH_TYPE=entra_default MCP_ENTRA_SCOPE=api://my-app/.default MCP_ENTRA_TENANT_ID=00000000-0000-0000-0000-000000000000 dotnet test
```

---

## 4. Agent Setup

The agent is an `IChatClient` with `FunctionInvokingChatClient` middleware. This middleware intercepts tool-call requests from the model, executes each `AIFunction`, feeds results back, and loops until a final text response.

`ChatClientFactory` (in `Library.Application.Factories`) is the single swap-point for the LLM provider:

```csharp
using Microsoft.Extensions.AI;
using OllamaSharp;

public static class ChatClientFactory
{
    private static readonly Uri OllamaEndpoint =
        new(Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434");

    private static readonly string Model =
        Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "gpt-oss:120b-cloud";

    public static IChatClient Create()
    {
        return new ChatClientBuilder(new OllamaApiClient(OllamaEndpoint, Model))
            .UseFunctionInvocation()
            .Build();
    }
}
```

| Variable | Default | Description |
|---|---|---|
| `OLLAMA_ENDPOINT` | `http://localhost:11434` | Ollama API base URL |
| `OLLAMA_MODEL` | `gpt-oss:120b-cloud` | Model name |

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
    public static IReadOnlyList<FunctionCallContent> GetToolCalls(ChatResponse response) =>
        response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionCallContent>()
            .ToList();

    public static IReadOnlyList<FunctionResultContent> GetToolResults(ChatResponse response) =>
        response.Messages
            .SelectMany(m => m.Contents)
            .OfType<FunctionResultContent>()
            .ToList();

    public static IReadOnlyList<string> GetCalledToolNames(ChatResponse response) =>
        GetToolCalls(response)
            .Select(c => c.Name)
            .Distinct()
            .ToList();

    public static void AssertToolWasCalled(ChatResponse response, string toolName)
    {
        var calls = GetToolCalls(response);
        Assert.Contains(calls, c => c.Name == toolName);
    }

    public static void AssertToolHasResult(ChatResponse response, string toolName)
    {
        var calls = GetToolCalls(response);
        var callIds = calls.Where(c => c.Name == toolName).Select(c => c.CallId).ToHashSet();
        var results = GetToolResults(response);
        Assert.Contains(results, r => callIds.Contains(r.CallId));
    }
}
```

---

## 8. Full xUnit Test Example

Tests use **collection fixtures** (not `IClassFixture`) so a single MCP server process is shared across all test classes in the collection. Every test logs its full agent response breakdown via `ResponseLogger`.

```csharp
[Collection(McpStdioCollection.Name)]
public class SecurityAgentTests : IDisposable
{
    private static readonly string[] DomainToolNames =
    [
        "query_security_checks",
        "query_networking_metrics",
        "query_packet_tracing",
        "query_security_analytics",
    ];

    private const string PromptName = "security-posture-assessment";

    private readonly McpStdioFixture _fixture;
    private readonly List<AITool> _tools;
    private readonly IChatClient _agent;
    private readonly ITestOutputHelper _output;

    public SecurityAgentTests(McpStdioFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _tools = fixture.Tools
            .Where(t => DomainToolNames.Contains(t.Name))
            .ToList<AITool>();
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
}
```

> To see the `ResponseLogger` output, run with `--logger "console;verbosity=detailed"`.

---

## 9. HTTP Test Example (Remote MCP Server)

The same `McpHttpFixture` works against any Streamable HTTP MCP server by setting `MCP_HTTP_ENDPOINT`:

```csharp
[Collection(McpHttpCollection.Name)]
public class MsLearnAgentTests : IDisposable
{
    private readonly McpHttpFixture _fixture;
    private readonly IChatClient _agent;
    private readonly ITestOutputHelper _output;

    public MsLearnAgentTests(McpHttpFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _agent = ChatClientFactory.Create();
    }

    public void Dispose() => (_agent as IDisposable)?.Dispose();

    [Fact]
    public async Task Agent_CallsMsLearnSearchTool()
    {
        var tools = _fixture.Tools.ToList<AITool>();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User,
                "Search for documentation on how to use IChatClient with tool calling in .NET.")
        };

        var response = await _agent.GetResponseAsync(messages, new() { Tools = tools });
        ResponseLogger.Log(_output, response);

        var calls = ResponseAssertions.GetToolCalls(response);
        Assert.NotEmpty(calls);
    }
}
```

```bash
MCP_HTTP_ENDPOINT=https://learn.microsoft.com/api/mcp dotnet test --filter MsLearnAgentTests
```

---

## 10. AI-Assisted Evaluation (Judge Pattern)

For complex scenarios where deterministic assertions (e.g., checking for a specific tool name) are insufficient, use the `McpEvaluationFixture`. This uses a "Judge LLM" (via `AgentEvaluator` service) to score the agent's response against a rubric.

### 10.1 `AgentRunResponse<T>`

Wrap the agent's output and message history to provide context to the judge:

```csharp
using Microsoft.Extensions.AI;
using Library.Domain.Models.Evaluation;

// Capture the agent run
var response = await agent.GetResponseAsync(messages, options);
var runResponse = AgentRunResponse<string>.FromText(response.Text, response.Messages);
```

### 10.2 Evaluation Fixture and Assertions

`McpEvaluationFixture` wraps the `AgentEvaluator` service. Construct it from the same `IChatClient` (Ollama or any provider) and the xUnit `ITestOutputHelper`. It automatically logs scores before assertions run.

Evaluations are made via fluent `.Expect()` chains — each accepts a `Func<EvaluationResult, bool>` predicate and a failure message. Tool calls and results from the agent run are included in the judge's context so rubrics can reference them.

```csharp
using Library.Domain.Models.Evaluation;
using Tests.Utilities.Fixtures;

// Wrap the agent's response
var runResponse = AgentRunResponse<string>.FromText(response.Text, response.Messages);

// Create fixture (judge reuses the same IChatClient as the agent)
var evalFixture = new McpEvaluationFixture(_agent, _output);

// Evaluate → then chain assertions
var result = await evalFixture.EvaluateAsync(
    systemPrompt: "You are a cloud security expert...",
    userInput: "Check the status of the firewall.",
    response: runResponse,
    rubric: "The agent must call a security tool and report whether the firewall is active."
);

evalFixture
    .Expect(result, r => r.Score.TaskSuccess > 0.8, "Task success score too low.")
    .Expect(result, r => r.Score.Hallucination < 0.2, "Hallucination score too high.");
```

> The judge prompt includes `[tool_call]` and `[tool_result]` blocks from `FunctionCallContent`/`FunctionResultContent` in `response.Messages`, so rubrics can assert on tool routing behaviour as well as answer quality.

---

## 11. Environment Variables

All configuration is done via environment variables — no hardcoded values in test code:

**Agent (ChatClientFactory):**

| Variable | Default | Description |
|---|---|---|
| `OLLAMA_ENDPOINT` | `http://localhost:11434` | Ollama API base URL |
| `OLLAMA_MODEL` | `gpt-oss:120b-cloud` | Model name |

**HTTP Transport (McpHttpFixture):**

| Variable | Default | Description |
|---|---|---|
| `MCP_HTTP_ENDPOINT` | `http://localhost:5000/mcp` | MCP server URL |
| `MCP_HTTP_NAME` | Derived from host | Friendly transport name |
| `MCP_AUTH_TYPE` | `none` | Auth strategy: `none`, `apikey`, `oauth`, or `entra_default` |

**API Key Auth** (when `MCP_AUTH_TYPE=apikey`):

| Variable | Default | Description |
|---|---|---|
| `MCP_API_KEY` | *(required)* | The API key value |
| `MCP_API_KEY_HEADER` | `Authorization` (as `Bearer {key}`) | Custom header name (e.g. `x-api-key`) |

**OAuth** (when `MCP_AUTH_TYPE=oauth`):

The fixture auto-probes the endpoint on startup (401 → RFC 9728 resource metadata → RFC 8414 AS metadata). If the AS advertises a `registration_endpoint`, dynamic registration is used and no client ID is needed. If not, `OAUTH_CLIENT_ID` is required.

| Variable | Default | Description |
|---|---|---|
| `OAUTH_CLIENT_ID` | *(auto-detected — required if no dynamic registration)* | OAuth client ID |
| `OAUTH_CLIENT_SECRET` | *(none — public client / PKCE)* | OAuth client secret |
| `OAUTH_SCOPES` | *(server-advertised scopes)* | Space-separated scopes |
| `OAUTH_REDIRECT_URI` | `http://localhost:8900/` | Loopback redirect URI |

**Entra ID** (when `MCP_AUTH_TYPE=entra_default`):

| Variable | Default | Description |
|---|---|---|
| `MCP_ENTRA_SCOPE` | *(required)* | Token scope (e.g. `api://my-app/.default`) |
| `MCP_ENTRA_TENANT_ID` | *(ambient)* | Azure AD tenant ID (optional — narrows credential chain) |

---

## 12. Recommended Testing Strategy

**CI:**
- STDIO mode (`McpStdioFixture` + `[Collection(McpStdioCollection.Name)]`)
- Deterministic tool assertions (`AssertToolWasCalled`)
- Domain-level tool filtering (all tools from a domain, not just one)
- MCP prompt injection
- `ResponseLogger.Log()` on every test for traceability (use `--logger "console;verbosity=detailed"`)

**Integration:**
- HTTP mode (`McpHttpFixture` + `[Collection(McpHttpCollection.Name)]`)
- Configurable endpoint via `MCP_HTTP_ENDPOINT` (local service or remote like `learn.microsoft.com/api/mcp`)
- Use `McpEvaluationFixture` (works with any Ollama model — no Azure required) for rubric-based quality assertions
- No mocks

