# MCP Authorization

Two controls are required: **hide tools** the caller shouldn't see (`tools/list`) and **block calls** even if a tool name is guessed (`tools/call`).

---

## HTTP: ASP.NET Auth + SDK Authorization Filters

`AddAuthorizationFilters()` makes `[Authorize]` metadata apply to MCP operations. ([SDK issue #968][5], [Medium][6])

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // configure authority/audience
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("scope:devices.read", p => p.RequireClaim("scope", "devices.read"));
});

builder.Services
    .AddMcpServer()
    .AddAuthorizationFilters()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapMcp().RequireAuthorization();
app.Run();
```

---

## Scoped Tool

```csharp
[McpServerToolType]
[Authorize(Policy = "scope:devices.read")]
public static class DeviceTools
{
    [McpServerTool(Name = "device.status")]
    public static string GetStatus(string deviceId) => $"ok:{deviceId}";
}
```

---

## STDIO: Manual Call-Time Auth

No `HttpContext.User` is available. Read a token from env/launch config, build a `ClaimsPrincipal`, and enforce in `WithCallToolHandler`.

---

## Resources and Prompts

Gate access inside handlers directly:

```csharp
if (!ctx.User.HasClaim("scope", "devices.read"))
    throw new UnauthorizedAccessException();
```

---

## References

[5]: https://github.com/modelcontextprotocol/csharp-sdk/issues/968
[6]: https://octelys.medium.com/setting-up-an-mcp-server-with-oauth-authentication-in-asp-net-core-a-complete-guide-135a59659e75
