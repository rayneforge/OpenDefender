Yes — here’s the **correct, official Microsoft + NuGet model** for releasing an **MCP server as a NuGet package** that clients like **VS Code, Visual Studio, GitHub Copilot, etc., can auto-discover and run**. This matches what you were thinking (`no builder.Services.Add…` and not just an ASP.NET host). ([Microsoft Learn][1])

---

## ✅ 1. NuGet MCP Server Package Concept

Microsoft (and the .NET ecosystem) **explicitly supports shipping MCP servers as NuGet packages** that consumeable MCP clients (like VS Code or Visual Studio) can discover and launch via standard IO (`stdio`) or other transports. ([Microsoft Learn][1])

That means your server **doesn’t need a DI builder hook** — instead you package the compiled server so IDEs and other clients can run it directly. ([Microsoft Learn][2])

---

## 📦 2. What a NuGet MCP Server Package Looks Like

As an example, the official MCP server package on NuGet is:

```
NuGet.Mcp.Server
```

This contains:

* An actual MCP server implementation
* A program that runs and exposes MCP tools
* A way for the client (VS Code/Visual Studio/etc.) to run it via `dnx` (`.NET 10` tool runner)
* A metadata snippet that MCP clients use to discover and hook into it ([NuGet][3])

You can install/run it locally like:

```bash
dotnet tool install --global NuGet.Mcp.Server --version 1.1.29
```

…or via:

```bash
dotnet new tool-manifest
dotnet tool install --local NuGet.Mcp.Server --version 1.1.29
```

And the standard MCP client snippet to configure it in `mcp.json` is:

```json
{
  "servers": {
    "nuget": {
      "type": "stdio",
      "command": "dnx",
      "args": [ "NuGet.Mcp.Server@1.1.29" ]
    }
  }
}
```

([NuGet][3])

---

## 🛠 Transport: `dnx` and MCP Clients

To enable clients to **launch the package automatically**, Microsoft introduced a tool called `dnx` as part of .NET 10 SDK:

* It will download the specified NuGet package if it’s not locally installed
* It runs it as an MCP server using standard IO
* MCP clients (VS Code, Visual Studio, Copilot) can launch the NuGet MCP server without extra manual setup ([Microsoft Learn][2])

Example:

```bash
dnx NuGet.Mcp.Server@1.1.29 --yes
```

This command tells `dnx` to acquire the MCP server package and run it. ([Microsoft Learn][1])

---

## 🔍 3. How Clients Discover It

MCP clients support several discovery methods:

### 🟥 Static config

In your `.vscode/mcp.json`:

```json
{
  "servers": {
    "nuget": {
      "type": "stdio",
      "command": "dnx",
      "args": [ "NuGet.Mcp.Server@1.1.29", "--yes" ]
    }
  }
}
```

### 🟩 IDE UI

Both Visual Studio and Visual Studio Code have tool pickers where MCP servers listed in configuration appear automatically once enabled by the user. ([Microsoft Learn][2])

### 🟦 Hosted MCP Discovery

If a client adds support for MCP server registry/discovery (as seen in spec discussions), the tooling can show MCP servers available on NuGet.org without manual config. ([GitHub][4])

---

## 🏷 Key Points to Publishing Your MCP Server Package

If you’re building your own MCP server and want it on NuGet:

### 1. Build the MCP server using the **.NET MCP SDK**

Use `Microsoft.McpServer` templates and SDK. ([Microsoft Learn][5])

### 2. Add a `.mcp/server.json` or equivalent manifest

This declares your server and tools so clients can list them.

### 3. Package it as a **NuGet .NET tool**

Set `PackAsTool=true` and appropriate metadata in csproj.

Example in `.csproj`:

```xml
<PropertyGroup>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>MyMcpServer</ToolCommandName>
  <PackageType>mcp-server</PackageType>
</PropertyGroup>
```

### 4. Publish to NuGet.org (or other feed)

Once it’s there, MCP clients can install/launch it automatically with `dnx` or local tool installation.

---

## 🧠 Summary

✔ NuGet **does support shipping MCP servers as discoverable tool packages** (not just libraries) ✨([Microsoft Learn][1])
✔ IDEs like VS Code and Visual Studio can launch these packages as MCP servers using `dnx` or local tool manifests ([Microsoft Learn][2])
✔ The server package is run by clients, not added via DI or builder APIs in the host app ([NuGet][3])

---

If you want, I can give you a real **starter template for a .NET MCP server NuGet tool (with .mcp manifest + csproj settings) that clients can auto-run** — just let me know!

[1]: https://learn.microsoft.com/en-us/nuget/concepts/nuget-mcp?utm_source=chatgpt.com "MCP servers in NuGet packages"
[2]: https://learn.microsoft.com/en-us/nuget/concepts/nuget-mcp-server?utm_source=chatgpt.com "Using the NuGet Model Context Protocol (MCP) Server"
[3]: https://www.nuget.org/packages/NuGet.Mcp.Server?utm_source=chatgpt.com "NuGet.Mcp.Server 1.1.19"
[4]: https://github.com/modelcontextprotocol/modelcontextprotocol/discussions/1160?utm_source=chatgpt.com "A Unified Approach for Installing MCP Servers in IDEs and ..."
[5]: https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-mcp-server?utm_source=chatgpt.com "Create a minimal MCP server and publish to NuGet - .NET"
