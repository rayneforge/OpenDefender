# OpenDefender

![OpenDefender: an AI emblem connected to security, performance, and system internals](docs/assets/opendefender-banner.png)

Read-only MCP tools that help an AI agent explain security and system health on a Windows device or Linux home server.

OpenDefender gives your existing AI assistant structured observations about the machine it runs on. Ask what is listening, which remote addresses are connected, whether the firewall is enabled, or which services need attention. The assistant explains the evidence and suggests a next step; you stay in control of changes.

**Status:** early-stage software. Windows builds and local MCP checks have been validated. CI defines Windows and Linux runtime checks. Coverage depends on the OS, installed utilities, and your account's permissions. Missing data is not proof that a device is secure.

## Quick start

Install the .NET 10 SDK and connect this server in your MCP client:

```json
{
  "servers": {
    "open-defender": {
      "type": "stdio",
      "command": "dnx",
      "args": ["Rayneforge.OpenDefender"]
    }
  }
}
```

This is VS Code's `.vscode/mcp.json` format. Other clients may use `mcpServers` instead of `servers`. `dnx` downloads and runs the NuGet tool without a global installation. It runs the version on your configured package feed; local source changes become available to users only after publication. Pin a tested published version with `Rayneforge.OpenDefender@<version>`.

The equivalent command is:

```bash
dotnet tool exec Rayneforge.OpenDefender
```

A permanent install is also available:

```bash
dotnet tool install -g Rayneforge.OpenDefender
```

Then use `rayneforge-opendefender` as the MCP command with empty `args`.

### Without a .NET installation

Download a Windows x64 or Linux x64 asset from [Releases](https://github.com/rayneforge/OpenDefender/releases), when available. Extract it and configure its executable directly:

```json
{
  "servers": {
    "open-defender": {
      "type": "stdio",
      "command": "C:/Tools/OpenDefender/Service.exe",
      "args": []
    }
  }
}
```

On Linux, use an absolute path such as `/opt/opendefender/Service`. These builds include the .NET runtime and native libraries. Normal OS runtime dependencies still apply. The executable can start from any working directory.

## Your first review

Start the server in your MCP client and ask:

> Review this device for a home-server owner. Explain what you observed, what you could not check, and the three most useful next steps. Do not change anything.

Other useful questions:

- Which TCP connections and listening ports are visible right now? Explain the local and remote addresses.
- Is the firewall reported as active? Tell me how fresh and complete that evidence is.
- Which services or scheduled tasks need investigation?
- What information is missing before you can assess this machine?

Background telemetry begins after startup. The first saved results take time to collect. `query_network_connections` reads a live TCP snapshot when called and does not save it to disk. Numeric IP addresses are not resolved to external services.

## What it can observe

| Area | Available tools and evidence |
| --- | --- |
| Security | `query_security_checks`, `query_security_analytics`: firewall, listening-port counts, failed-login observations and derived flags |
| Connections | `query_network_connections`: current TCP local/remote addresses, ports, and state; `query_networking_metrics`: interface byte counters |
| Reliability | Service states, scheduled tasks, filesystem/mount observations, and derived reliability signals |
| Device health | CPU, memory, load/queue, kernel/OS, optional hardware and GPU probes |
| Logging | Log usage/inventory and derived logging signals |
| Collection | `query_orchestrations`: saved collection timestamps |

The connection tool can show connected peers and listeners. It does not reconstruct past traffic, capture payloads, identify who initiated a connection, measure bytes per peer, or prove internet exposure. UDP visibility is not implemented in that tool yet. A filesystem being mounted does not establish that a backup can be restored. An analytics threshold flag does not establish compromise.

## Permissions and privacy

Run as an ordinary user. OpenDefender does not request elevation, invoke `sudo`, change firewall rules, restart services, install system utilities, or execute agent-supplied shell commands. Collectors run fixed OS probes. MCP tools query telemetry or inspect current TCP endpoints. All tools advertise read-only, non-destructive, local behavior; these annotations describe behavior and are not a security sandbox.

Stdio is the only transport: the MCP client starts a child process and talks over stdin/stdout. The executable has no HTTP host or API routes. No built-in LLM is required. Logs go to stderr.

Packet sampling is optional and off by default. The existing Linux `tcpdump` probe retains only a count, not packets, and requires permissions already granted to the process. Set `Service__EnablePacketCapture=true` only if you intend to enable that probe. On Windows the corresponding probe is a connection-count proxy. The live connection tool is available without this option.

Saved telemetry uses `%LOCALAPPDATA%/OpenDefender` on Windows, normally `~/.local/share/OpenDefender` on Linux. Override with `OPENDEFENDER_DATA_DIR`. Default telemetry retention is two hours, with a purge scheduled every thirty minutes. Live connection snapshots are not stored. SQLite files are unencrypted; retention deletes rows but does not guarantee physical erasure, removal from backups, or shrinking files. Purges run only while the service is running. Older `.data` databases are not moved automatically.

Telemetry can contain host details, addresses, and service names. Your MCP client or its AI provider can receive tool results. Review that client's data handling before connecting a sensitive machine. OpenDefender does not upload snapshots itself in the default configuration. Optional built-in LLM agents can send telemetry to a provider if you explicitly configure `Service:Llm`.

Some probes need permissions or optional utilities and cannot work for every ordinary account. Several legacy probes still collapse errors into zero/empty values; consistent unknown/error reporting is a priority before treating reports as authoritative. See [planned improvements](docs/design/roadmap.md) and the [security policy](SECURITY.md).

## Development and validation

Requirements: .NET 10 SDK; Windows or Linux; PowerShell 7 for the protocol smoke script. Linux probes use `/bin/bash`, with tools such as `ss`, `ip`, and `journalctl`; hardware probes are optional.

```bash
dotnet build src/ObservabilityStack.slnx -c Release
dotnet test src/Tests/Tests.csproj -c Release --no-build --filter "Category=WindowsCommand|Category=Process|Category=NetworkConnection"
```

On Linux, replace `WindowsCommand` with `LinuxCommand`. Agent evaluation tests use external LLMs and are separate from these deterministic checks.

Publish standalone binaries:

```bash
dotnet publish src/Service/Service.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o publish/win-x64
dotnet publish src/Service/Service.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -o publish/linux-x64
```

Validate a published executable:

```powershell
./scripts/Test-McpStdio.ps1 -PublishDirectory publish/win-x64
```

Validate a newly packed NuGet tool through the same one-shot execution used by `dnx`:

```powershell
dotnet pack src/Service/Service.csproj -c Release -o publish/nupkg -p:Version=1.0.0-validation
./scripts/Test-McpStdio.ps1 -PublishDirectory publish/win-x64 -PackageDirectory publish/nupkg -PackageVersion 1.0.0-validation
```

Use a fresh validation version after each package change to avoid reusing a cached package. CI builds both OS targets, runs their platform tests, and checks the standalone and packaged stdio paths. Manual dispatch publishes release assets and NuGet packages.

Optional configuration is read beside the executable; environment variables and command-line arguments override it. Legacy HTTP transport settings are rejected.

## Project layout

- `src/Service`: Stdio MCP tools/prompts and background services.
- `src/Library`: fixed collectors, typed models, query helpers, SQLite storage, and optional built-in agents.
- `src/Tests`: platform probes, parsing/process checks, and separate LLM evaluations.
- `.github/agents`: optional Copilot roles for security, reliability, device health, and logging.
- `docs`: design notes and reference material. Some legacy reference documents describe earlier behavior; the executable and this README define the current quick start.

[Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Roadmap](docs/design/roadmap.md) · [MIT license](LICENSE)

Implementation references: [MCP stdio](https://modelcontextprotocol.io/specification/2025-11-25/basic/transports), [MCP tools](https://modelcontextprotocol.io/specification/2025-11-25/server/tools), [.NET tool execution](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-exec), and [.NET single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).
