# OpenDefender 🛡️

**OpenDefender** is a device observability and AI agent enablement platform built on **.NET 10**. Its primary purpose is to eliminate repetitive manual command execution by giving AI agents structured, read-only visibility into system state — security posture, infrastructure health, reliability, and telemetry — through a typed MCP interface.

Rather than granting agents broad shell access, OpenDefender collects, stages, and surfaces system telemetry through well-defined domain boundaries. Each agent role has a clearly scoped view of the data it owns, and all agents are read-only by design.

---

## Problem It Solves

Effective device monitoring requires constantly running shell commands, correlating outputs, and interpreting trends manually. OpenDefender automates that collection pipeline and surfaces the results through an MCP server so that AI agents (GitHub Copilot, etc.) can answer questions like:

- _"Is the disk expected to fill in the next 48 hours?"_
- _"Are there any firewall rules or open ports that have changed since the last run?"_
- _"Are all my scheduled backup jobs completing successfully?"_
- _"Is the system journal at risk of breaching the 180-day retention requirement?"_

...without ever running a shell command themselves.

---

## Architecture

The system has three layers:

```
┌─────────────────────────────────────────────────────────────┐
│  COLLECTION  (DiagnosticOrchestrator)                       │
│  Runs shell probes: top, free, smartctl, journalctl, etc.   │
│  Writes raw metrics → ReportDbContext (SQLite)              │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│  STAGING  (AnalyticsOrchestrator)                           │
│  Computes deltas, growth rates, breach flags, gap detection │
│  Writes derived analytics → AnalyticsDbContext (SQLite)     │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│  CONSUMPTION  (MCP Server + OData API)                      │
│  Exposes raw + derived data to agents via structured tools  │
└─────────────────────────────────────────────────────────────┘
```

Collection and staging run automatically on startup (if the database is empty) and on a recurring schedule via background hosted services.

---

## Agent System

OpenDefender is purpose-built to serve four specialized AI agent roles. Each agent has a defined domain, owns a specific subset of MCP tools, and is strictly read-only.

### `@core` — Platform & Workload
Observes the physical and logical foundation: hardware health, kernel stability, resource utilization, and GPU/accelerator monitoring.

| Tool | Data |
|---|---|
| `query_resource_metrics` | CPU, memory, disk, swap vs. thresholds |
| `query_hardware_metrics` | Device health, temperature, SMART attributes |
| `query_kernel_metrics` | Kernel version, security params, boot metrics |
| `query_gpu_metrics` | GPU utilization, VRAM, thermals |
| `query_resource_analytics` | Derived deltas, growth rates, breach detection |

Prompt: `infrastructure-health-check`

---

### `@shield` — Security & Connectivity
Observes the defensive perimeter: access control, network integrity, firewall posture, and traffic patterns.

| Tool | Data |
|---|---|
| `query_security_checks` | Firewall state, open ports, severity flags |
| `query_networking_metrics` | Interface IPs, link state, traffic counters |
| `query_packet_tracing` | Active captures, anomaly indicators |
| `query_security_analytics` | Derived breach flags, new issue counts |

Prompt: `security-posture-assessment`

---

### `@anchor` — Reliability & Recovery
Observes the continuity posture: backup chain integrity, service stability, scheduled job health, and disaster recovery readiness.

| Tool | Data |
|---|---|
| `query_data_recovery` | Backup target availability, mount state, size |
| `query_service_metrics` | Service lifecycle state, uptime |
| `query_automation_metrics` | Timer/job health, automation results |
| `query_control_map` | Control layer status, required actions |
| `query_reliability_analytics` | Derived degradation detection, restart flags |

Prompt: `reliability-stability-review`

---

### `@ledger` — Logging & Telemetry
Observes the evidence pipeline: log completeness, retention compliance, shipping health, and coverage gaps.

| Tool | Data |
|---|---|
| `query_logging_metrics` | Journal disk usage, pipeline component health |
| `query_logging_inventory` | Log source inventory, types, sizes |
| `query_ledger_analytics` | Growth trends, retention compliance, gap flags |

Prompt: `logging-retention-audit`

---

### Shared Cross-Reference Tool

All agents may cross-reference `query_control_map` and `query_orchestrations` to understand the current control-layer status and last collection run.

### Severity Classification

All agents classify findings consistently:

| Level | Meaning |
|---|---|
| **S1** | Critical — immediate action required (e.g. active breach, RPO/RTO at risk, auth logs near loss) |
| **S2** | High |
| **S3** | Medium |
| **S4** | Informational |

---

## Project Structure

```text
src/
├── Library/
│   ├── Application/Services/   # Collectors (DiagnosticOrchestrator, AnalyticsOrchestrator)
│   ├── Database/               # ReportDbContext (raw) + AnalyticsDbContext (derived)
│   ├── Domain/                 # Strongly-typed models for all metrics and analytics
│   └── Infrastructure/         # QueryHelper, EF Core helpers
├── Service/
│   ├── Mcp/                    # MCP tool + prompt implementations (per agent domain)
│   ├── Controllers/            # OData controllers (raw + analytics routes)
│   ├── Services/               # Background hosted services (collection + retention)
│   └── Program.cs              # Startup — Stdio or HTTP transport
├── Cli/                        # Manual diagnostic runner
└── Tests/
    └── Mcp/                    # Agent-level integration tests
```

```text
.github/
└── agents/
    ├── open-defneder.core.agent.md     # @core agent definition
    ├── open-defender.shield.agent.md   # @shield agent definition
    ├── open-defender.anchor.agent.md   # @anchor agent definition
    └── open-defender.ledger.agent.md   # @ledger agent definition
```

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- Linux (collection probes use Linux system commands)

### Run as MCP Server (Stdio — for GitHub Copilot / VS Code)

Ensure `TransportType: "Stdio"` in [src/Service/appsettings.json](src/Service/appsettings.json), then add to `.vscode/mcp.json`:

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

On first run with an empty database, the service automatically runs a full collection and analytics cycle before becoming available to agents.

### Run as HTTP Service (for OData browsing / debugging)

Set `TransportType: "Http"` and run:

```bash
dotnet run --project src/Service/Service.csproj
```

Browse the intelligence layer directly:
- Raw metrics: `http://localhost:5000/odata/metrics/ResourceMetrics`
- Derived analytics: `http://localhost:5000/odata/analytics/SecurityAnalytics`
- Full entity list: `ResourceMetrics`, `HardwareMetrics`, `KernelMetrics`, `GpuMetrics`, `SecurityChecks`, `NetworkingMetrics`, `PacketTracingMetrics`, `LoggingMetrics`, `LoggingInventoryMetrics`, `ServiceMetrics`, `AutomationMetrics`, `DataRecoveryMetrics`, `ControlMap`, `ResourceAnalytics`, `SecurityAnalytics`, `LedgerAnalytics`, `ReliabilityAnalytics`

### Manual Diagnostic Run (CLI)

```bash
dotnet run --project src/Cli/Cli.csproj
```

---

## Agent Constraints (Enforced by Design)

- **All agents are read-only.** No agent may restart services, modify config, rotate credentials, vacuum logs, or change any system state. They observe and flag only.
- **Domains are isolated.** Each agent owns its tools. Cross-domain investigations require delegating to the appropriate agent.
- **Recommendations are always explicit.** When an agent flags an issue, it recommends a specific action — but the owner or an authorized process executes it.

---

## CI/CD

A [GitHub Actions workflow](.github/workflows/build.yml) builds and packages self-contained executables for Linux and Windows on every push to `main`.
