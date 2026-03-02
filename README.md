# Observability Stack 🛡️

A multi-layered observability and diagnostic system built with **.NET 10**, featuring automated collection, a derived analytics staging layer, and OData-powered intelligence.

## 🏗️ Architecture

The system is divided into three primary layers to ensure separation of concerns and data integrity:

1.  **Raw Collection Layer**: Executes low-level shell commands (top, free, journalctl, etc.) to gather real-time system metrics.
2.  **Analytics Staging Layer**: Processes raw metrics using the `AnalyticsOrchestrator` to compute high-level insights like deltas, growth rates, and policy breaches (e.g., retention compliance).
3.  **Consumption Layer**: Exposes both raw and derived data via **OData v4** endpoints for roles (Shield, Anchor, etc.) to query.

## 📁 Project Structure

```text
src/
├── Library/          # Shared domain models, DB contexts, and core logic
│   ├── Application/  # Collectors and Orchestration services
│   ├── Database/     # ReportDbContext (Raw) and AnalyticsDbContext (Derived)
│   └── Domain/       # Strongly typed OData-compatible models
├── Service/          # ASP.NET Core host for the Background Services and API (MCP/Stdio)
└── Cli/              # Diagnostic CLI tool for testing and manual runs
```

## 🚀 Getting Started

### Prerequisites

- .NET 10 SDK
- SQLite (Self-provisioning)

### Build & Run

**As an HTTP Service**:
Configure `TransportType: "Http"` in [appsettings.json](src/Service/appsettings.json) and run:
```bash
dotnet run --project src/Service/Service.csproj
```
Access the dashboard via [http://localhost:5000/odata/analytics/ResourceAnalytics](http://localhost:5000/odata/analytics/ResourceAnalytics).

**As a Stdio/MCP Service**:
Configure `TransportType: "Stdio"` and use it as a headless background process. Logs are redirected to `stderr`.

### CLI Testing
To perform a full diagnostic and analytics cycle manually:
```bash
dotnet run --project src/Cli/Cli.csproj
```

## 📊 API Endpoints

The service provides dual OData routes:

- **Raw Metrics**: `/odata/metrics/[Entity]` (e.g., `ResourceMetrics`, `LoggingInventoryMetrics`)
- **Derived Analytics**: `/odata/analytics/[Entity]` (e.g., `LedgerAnalytics`, `SecurityAnalytics`)

---

## 🧩 VS Code MCP Integration

OpenDefender is packaged as `Rayneforge.OpenDefender`, a .NET MCP server tool. Once published, VS Code and GitHub Copilot can discover and run it automatically.

### From NuGet (once published)

Install the tool and add the server to your `.vscode/mcp.json`:

```json
{
  "servers": {
    "open-defender": {
      "type": "stdio",
      "command": "dnx",
      "args": ["Rayneforge.OpenDefender@1.0.0"]
    }
  }
}
```

### From source (local development)

Point directly at the project:

```json
{
  "servers": {
    "open-defender": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/solutions/observability/src/Service/Service.csproj"
      ]
    }
  }
}
```

> **Note:** The service defaults to `TransportType: "Stdio"` in `appsettings.json`. Ensure this is set when running as an MCP server.

---

## 🛠️ CI/CD
The project includes a [GitHub Workflow](.github/workflows/build.yml) that automatically builds and packages self-contained executables for **Windows** and **Linux** on every push to `main`.
