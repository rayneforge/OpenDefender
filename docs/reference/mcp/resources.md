# MCP Resources

Resources expose readable data via handlers: list, read, templates, and subscriptions.

---

## Overview

| Primitive    | Purpose                 | Protocol Method  | DDD Analogy |
| ------------ | ----------------------- | ---------------- | ----------- |
| **Resource** | Provide structured data | `resources/read` | Query       |

---

## Register Handlers

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithListResourceTemplatesHandler(ListTemplates)
    .WithListResourcesHandler(ListResources)
    .WithReadResourceHandler(ReadResource)
    .WithToolsFromAssembly();
```

---

## Resource Templates

Publish URI templates so clients can construct resource URIs consistently.

```csharp
static Task<ListResourceTemplatesResult> ListTemplates(
    RequestContext<ListResourceTemplatesRequestParams> ctx,
    CancellationToken ct)
{
    return Task.FromResult(new ListResourceTemplatesResult
    {
        ResourceTemplates =
        [
            new ResourceTemplate
            {
                Name = "device-telemetry",
                UriTemplate = "device://{id}/telemetry",
                Description = "Telemetry stream for a device"
            }
        ]
    });
}
```

---

## List Resources

Return concrete resource items available for the session.

```csharp
static Task<ListResourcesResult> ListResources(
    RequestContext<ListResourcesRequestParams> ctx,
    CancellationToken ct)
{
    return Task.FromResult(new ListResourcesResult
    {
        Resources =
        [
            new Resource
            {
                Name = "device-1-telemetry",
                Uri = "device://device-1/telemetry",
                Description = "Telemetry for device-1"
            }
        ]
    });
}
```

---

## Read Resource

Return content for a requested URI.

```csharp
static Task<ReadResourceResult> ReadResource(
    RequestContext<ReadResourceRequestParams> ctx,
    CancellationToken ct)
{
    var uri = ctx.Params.Uri;

    if (uri == "device://device-1/telemetry")
    {
        return Task.FromResult(new ReadResourceResult
        {
            Contents =
            [
                new ResourceContents
                {
                    Text = """
                    {
                        "temperature": 72.3,
                        "battery": 91
                    }
                    """
                }
            ]
        });
    }

    throw new Exception("Unknown resource");
}
```

---

## Handler Hooks

| Handler                               | Purpose                           |
| ------------------------------------- | --------------------------------- |
| `WithListResourcesHandler`            | Customize `resources/list`        |
| `WithReadResourceHandler`             | Handle `resources/read`           |
| `WithListResourceTemplatesHandler`    | Handle `resources/templates/list` |
| `WithSubscribeToResourcesHandler`     | Handle resource subscription      |
| `WithUnsubscribeFromResourcesHandler` | Handle resource unsubscription    |

([SDK discussion #669][8], [Systenics AI][9])

> **Note:** There are sharp edges around overriding list handlers. Test with the MCP Inspector and your host carefully. ([SDK issue #520][7])

---

## References

[7]: https://github.com/modelcontextprotocol/csharp-sdk/issues/520
[8]: https://github.com/modelcontextprotocol/csharp-sdk/discussions/669
[9]: https://systenics.ai/blog/2025-04-10-building-a-model-context-protocol-server-with-net-and-semantic-kernel-integration
