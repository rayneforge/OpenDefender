# MCP Tools

Tools are functions the model can invoke. Defined via attributes and discovered automatically.

---

## Overview

| Primitive | Purpose                    | Protocol Method | DDD Analogy |
| --------- | -------------------------- | --------------- | ----------- |
| **Tool**  | Execute logic/side effects | `tools/call`    | Command     |

---

## Define a Tool

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class EchoTools
{
    [McpServerTool(Name = "echo", Title = "Echo Tool")]
    [Description("Echoes the message back to the caller.")]
    public static string Echo([Description("Text to echo")] string message)
        => $"Echo: {message}";
}
```

Registration is automatic when using `.WithToolsFromAssembly()`. ([DevBlogs][4])

---

## Handler Hooks

| Handler                | Purpose                              |
| ---------------------- | ------------------------------------ |
| `WithListToolsHandler` | Filter or customize `tools/list`     |
| `WithCallToolHandler`  | Intercept `tools/call` (auth, audit) |

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport()

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

    .WithToolsFromAssembly();
```

([SDK issue #520][7])

---

## References

[4]: https://devblogs.microsoft.com/dotnet/mcp-csharp-sdk-2025-06-18-update/
[7]: https://github.com/modelcontextprotocol/csharp-sdk/issues/520
