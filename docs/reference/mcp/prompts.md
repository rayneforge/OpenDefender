# MCP Prompts

Prompts are reusable prompt templates fetched via `prompts/get`. Defined as attributed classes and registered with `.WithPrompts<T>()` or `.WithPromptsFromAssembly()`.

---

## Overview

| Primitive  | Purpose                   | Protocol Method | DDD Analogy          |
| ---------- | ------------------------- | --------------- | -------------------- |
| **Prompt** | Reusable prompt templates | `prompts/get`   | Policy / Instruction |

---

## Define a Prompt

```csharp
using ModelContextProtocol.Server;

[McpServerPromptType]
public static class DiagnosticPrompts
{
    [McpServerPrompt(
        Name = "device-diagnostic",
        Title = "Device Diagnostic Prompt",
        Description = "Analyze device telemetry and provide insights")]
    public static string CreateDiagnosticPrompt(
        string deviceId,
        string telemetryJson)
    {
        return $"""
        You are a systems diagnostics assistant.

        Device ID: {deviceId}

        Telemetry:
        {telemetryJson}

        Provide:
        - Health summary
        - Risk flags
        - Recommended actions
        """;
    }
}
```

---

## Register Prompts

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithPromptsFromAssembly()   // discovers all [McpServerPromptType] classes
    .WithToolsFromAssembly();
```

Or register a specific prompt type:

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithPrompts<DiagnosticPrompts>()
    .WithToolsFromAssembly();
```

---

## Handler Hooks

The SDK exposes handler overrides for prompts, matching the pattern used by tools and resources:

| Handler                    | Delegate Signature                                                       | Purpose                    |
| -------------------------- | ------------------------------------------------------------------------ | -------------------------- |
| `WithListPromptsHandler`   | `McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>`         | Customize `prompts/list`   |
| `WithGetPromptHandler`     | `McpRequestHandler<GetPromptRequestParams, GetPromptResult>`             | Intercept `prompts/get`    |

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithListPromptsHandler(async (ctx, ct) =>
    {
        // Filter prompts by session/scope
        return new ListPromptsResult { Prompts = [ /* ... */ ] };
    })
    .WithGetPromptHandler(async (ctx, ct) =>
    {
        // Intercept prompt retrieval
        var name = ctx.Params!.Name;
        return new GetPromptResult { Messages = [ /* ... */ ] };
    })
    .WithPromptsFromAssembly();
```

---

## Client-Side: Fetching Prompts

Use `McpClient.GetPromptAsync()` to fetch a prompt from the server. The result converts to `ChatMessage` objects for injection into a conversation:

```csharp
using ModelContextProtocol;
using ModelContextProtocol.Client;
using Microsoft.Extensions.AI;

var promptResult = await mcpClient.GetPromptAsync("device-diagnostic", new()
{
    ["deviceId"] = "device-1",
    ["telemetryJson"] = "{ ... }"
});

// Convert to ChatMessage list for use with IChatClient
IList<ChatMessage> messages = promptResult.ToChatMessages();
```

> `GetPromptResult.ToChatMessages()` returns `IList<ChatMessage>` (not `List<ChatMessage>`).

---

## References

[1]: https://github.com/modelcontextprotocol/csharp-sdk
