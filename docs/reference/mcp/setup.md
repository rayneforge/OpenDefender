# MCP Server Setup

## Install Packages

### HTTP Transport (Streamable HTTP / SSE)

```bash
dotnet new web -n MyMcpServer
cd MyMcpServer
dotnet add package ModelContextProtocol
dotnet add package ModelContextProtocol.AspNetCore
```

### STDIO Transport (local tool server)

Same core `ModelContextProtocol` package. No ASP.NET pipeline — `SessionId` may be null. ([SDK docs][3])

---

## Minimal Server Setup

```csharp
using ModelContextProtocol;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapMcp();
app.Run();
```

`.WithToolsFromAssembly()` auto-discovers all `[McpServerToolType]` classes in the current assembly. ([Microsoft Security Blog][2])

---

## Handler Hooks

Server builder handler overrides for interception and custom logic:

### Tools

| Handler                | Purpose                              |
| ---------------------- | ------------------------------------ |
| `WithListToolsHandler` | Filter or customize `tools/list`     |
| `WithCallToolHandler`  | Intercept `tools/call` (auth, audit) |

([SDK issue #520][7])

### Resources

| Handler                               | Purpose                           |
| ------------------------------------- | --------------------------------- |
| `WithListResourcesHandler`            | Customize `resources/list`        |
| `WithReadResourceHandler`             | Handle `resources/read`           |
| `WithListResourceTemplatesHandler`    | Handle `resources/templates/list` |
| `WithSubscribeToResourcesHandler`     | Handle resource subscription      |
| `WithUnsubscribeFromResourcesHandler` | Handle resource unsubscription    |

([SDK discussion #669][8], [Systenics AI][9])

### Prompts

| Handler                    | Purpose                  |
| -------------------------- | ------------------------ |
| `WithListPromptsHandler`   | Customize `prompts/list` |
| `WithGetPromptHandler`     | Intercept `prompts/get`  |

> **Note:** There are sharp edges around overriding list handlers (e.g., custom list tools behavior). Test with the MCP Inspector and your host carefully. ([SDK issue #520][7])

---

## Session Continuity

### SDK Behavior

`McpSession.SessionId` is available but transport-dependent: ([SDK docs][3])

| Transport | `SessionId` | Reliable? |
| --------- | ----------- | --------- |
| HTTP/SSE  | Populated   | Yes       |
| STDIO     | May be null | No        |

Protocol-level session semantics for STDIO remain under discussion. ([GitHub issue #1359][10])

### Recommended Pattern

Track your own correlation alongside the SDK session:

```json
{
  "SessionId": "<from SDK, when present>",
  "ClientId": "<your own, always>",
  "ToolOrResource": "device.status",
  "Timestamp": "2026-03-01T12:00:00Z"
}
```

* Use `SessionId` when available (HTTP).
* Always generate or require a `ClientId` you control.
* Never rely on STDIO session continuity.

---

## Full Wired Example

A single `Program.cs` with tools (attribute-discovered), resources (handler-based), prompts, and handler interception:

```csharp
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpTransport()

    // Tool interception
    .WithListToolsHandler(async (ctx, ct) =>
    {
        // Filter tools by session/scope
        return await Handlers.ListTools(ctx, ct);
    })
    .WithCallToolHandler(async (ctx, ct) =>
    {
        // Authorize, audit, then execute
        return await Handlers.CallTool(ctx, ct);
    })

    // Resources
    .WithListResourceTemplatesHandler(async (ctx, ct) => await Handlers.ListResourceTemplates(ctx, ct))
    .WithListResourcesHandler(async (ctx, ct) => await Handlers.ListResources(ctx, ct))
    .WithReadResourceHandler(async (ctx, ct) => await Handlers.ReadResource(ctx, ct))
    .WithSubscribeToResourcesHandler(async (ctx, ct) => await Handlers.Subscribe(ctx, ct))
    .WithUnsubscribeFromResourcesHandler(async (ctx, ct) => await Handlers.Unsubscribe(ctx, ct))

    // Prompts
    .WithListPromptsHandler(async (ctx, ct) => await Handlers.ListPrompts(ctx, ct))
    .WithGetPromptHandler(async (ctx, ct) => await Handlers.GetPrompt(ctx, ct))
    .WithPromptsFromAssembly()

    // Attribute-based tool discovery
    .WithToolsFromAssembly();

var app = builder.Build();
app.MapMcp();
app.Run();
```

> All handler hooks use the `McpRequestHandler<TParams, TResult>` delegate, where the first parameter is `RequestContext<TParams>` and the second is `CancellationToken`.

---

## Ship Checklist

- [ ] Transport configured: `AddMcpServer().WithHttpTransport()` or STDIO
- [ ] Endpoints mapped: `MapMcp()`
- [ ] Tools defined: `[McpServerToolType]` + `[McpServerTool]` with `[Description]`
- [ ] Tools registered: `.WithToolsFromAssembly()` or manual
- [ ] Auth (HTTP): `AddAuthorizationFilters()` + `[Authorize]` policies
- [ ] Auth (STDIO): `WithCallToolHandler` with manual enforcement
- [ ] Resources: list/read/templates handlers registered
- [ ] Prompts: `.WithPromptsFromAssembly()` or `.WithPrompts<T>()` for each prompt type
- [ ] Session tracking: `SessionId` when present + own `ClientId` always
- [ ] Interception: `WithCallToolHandler` for audit/scope gating

---

## References

| # | Source | Link |
|---|--------|------|
| 1 | MCP C# SDK (GitHub) | https://github.com/modelcontextprotocol/csharp-sdk |
| 2 | Microsoft Security Blog — Secure MCP in .NET | https://techcommunity.microsoft.com/blog/microsoft-security-blog/secure-model-context-protocol-mcp-implementation-with-azure-and-local-servers/4449660 |
| 3 | McpSession API Docs | https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.McpSession.html |
| 7 | SDK Issue #520 — HttpContext in Handlers | https://github.com/modelcontextprotocol/csharp-sdk/issues/520 |
| 8 | SDK Discussion #669 — Dynamic Tools | https://github.com/modelcontextprotocol/csharp-sdk/discussions/669 |
| 9 | Systenics AI — MCP + Semantic Kernel | https://systenics.ai/blog/2025-04-10-building-a-model-context-protocol-server-with-net-and-semantic-kernel-integration |
| 10 | Protocol Sessions Discussion | https://github.com/modelcontextprotocol/modelcontextprotocol/issues/1359 |

[2]: https://techcommunity.microsoft.com/blog/microsoft-security-blog/secure-model-context-protocol-mcp-implementation-with-azure-and-local-servers/4449660
[3]: https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.McpSession.html
[7]: https://github.com/modelcontextprotocol/csharp-sdk/issues/520
[8]: https://github.com/modelcontextprotocol/csharp-sdk/discussions/669
[9]: https://systenics.ai/blog/2025-04-10-building-a-model-context-protocol-server-with-net-and-semantic-kernel-integration
[10]: https://github.com/modelcontextprotocol/modelcontextprotocol/issues/1359
