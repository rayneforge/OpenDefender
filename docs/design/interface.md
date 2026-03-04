# OpenDefender — Interface Design

> Web dashboard for the OpenDefender observability stack.
> Consumes the Service API (OData + SSE/SignalR) and optionally talks to agents via MCP-over-HTTP.

---

## 1) Design Principles

- **Data-driven, not alert-driven** — this is an observability dashboard, not a SOC.
  The primary view is system health posture, not threat triage.
- **Agent-centric** — the four agents (Shield, Anchor, Core, Ledger) are first-class navigation items,
  not hidden behind generic "incidents".
- **Real-time optional** — the dashboard works fully from OData polling.
  SignalR adds live push for trigger completions and agent chat.
- **No auth wall initially** — single-user local tool. Auth comes later when multi-tenant is needed.

---

## 2) Information Architecture

### 2.1 Navigation (sidebar)

| Item | Maps to | Content |
|---|---|---|
| **Overview** | Orchestrations + all Analytics | System health summary |
| **Shield** | SecurityAnalytics + SecurityChecks | Perimeter, policy drift, firewall |
| **Anchor** | ReliabilityAnalytics + ServiceMetrics + DataRecoveryMetrics | Backups, service stability, drift |
| **Core** | ResourceAnalytics + HardwareMetrics + GpuMetrics + KernelMetrics | CPU, memory, disk, GPU, firmware |
| **Ledger** | LedgerAnalytics + LoggingMetrics + LoggingInventoryMetrics | Log pipelines, retention, coverage |
| **Metrics** | All 14 OData metric entity sets | Raw metric explorer (table/filter) |
| **Scheduler** | TaskRegistry + AgentTasks (bookmarks) | Trigger schedule, last run, next due |
| **Settings** | Service config / LLM config | Transport, LLM provider, intervals |

### 2.2 Overview page — layout

```
┌─────────────────────────────────────────────────────────────────────┐
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐              │
│  │ Shield   │ │ Anchor   │ │ Core     │ │ Ledger   │   KPI cards  │
│  │ ● OK     │ │ ▲ 2 Warn │ │ ● OK     │ │ ▲ 1 Warn │  (from      │
│  │ last: 2h │ │ last: 2h │ │ last: 4h │ │ last: 2h │   Analytics) │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘              │
│                                                                     │
│  ┌─────────────────────────────┐  ┌──────────────────────────────┐ │
│  │ Recent Trigger Activity     │  │ System Resource Gauge        │ │
│  │                             │  │                              │ │
│  │ ● daily_backup_check  2h   │  │  CPU ████████░░  72%         │ │
│  │ ● daily_pipeline_check 2h  │  │  MEM ██████░░░░  58%         │ │
│  │ ● daily_resource_check 4h  │  │  DSK █████████░  89%  ▲      │ │
│  │ ○ weekly_drift_check  5d   │  │  GPU ████░░░░░░  38%         │ │
│  │ ○ weekly_policy_drift 5d   │  │                              │ │
│  └─────────────────────────────┘  └──────────────────────────────┘ │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │ Orchestration Timeline (last 24h)                               ││
│  │ ──●──────●──────●──────●──────●──────●──────●──────────────>   ││
│  │   ^run1   ^run2  ^run3  ^run4  ^run5  ^run6  ^run7             ││
│  └─────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────┘
```

### 2.3 Agent detail page (e.g., Shield)

```
┌─────────────────────────────────────────────────────────────────────┐
│  Shield Agent                                            [Run Now] │
│                                                                     │
│  ┌─ Health Brief ─────────────────────────────────────────────────┐ │
│  │ Last weekly brief: 2026-03-01 04:00 UTC                       │ │
│  │ Level: OK | Detail: "Perimeter stable. No policy drift..."    │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌─ Triggers ─────────────┐  ┌─ Security Checks (OData) ────────┐ │
│  │ daily_perimeter_audit  │  │ ✓ firewall_active     PASS       │ │
│  │  Last: 2h ago  Next:22h│  │ ✓ selinux_enforcing   PASS       │ │
│  │ weekly_policy_drift    │  │ ✗ kptr_restrict        FAIL       │ │
│  │  Last: 5d ago  Next:2d │  │ ✓ auth_failures < 10  PASS       │ │
│  │ weekly_health_brief    │  │ ✓ no_open_ports        PASS       │ │
│  │  Last: 2d ago  Next:5d │  │                                   │ │
│  └────────────────────────┘  └───────────────────────────────────┘ │
│                                                                     │
│  ┌─ SecurityAnalytics History ────────────────────────────────────┐ │
│  │ [chart: security score over time, pass/fail ratio trend]      │ │
│  └────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.4 Agent Chat panel (slide-out, same as mockup concept)

This is the one piece the mockup got right — a chat sidebar.
But instead of SOC alerts, it's **agent conversation**:

- User types a natural language question
- Routed to the appropriate agent (or user picks one)
- Agent runs via `IChatClient` with MCP tools
- Response streams back via SignalR

This is why **SignalR over SSE** — the chat panel needs bidirectional comms.
SSE alone can push results, but the chat input requires a persistent
channel for streaming token-by-token responses.

---

## 3) Data Flow

```
┌──────────────┐    OData (poll)     ┌──────────────┐
│              │ ◄────────────────── │              │
│   Web App    │    SignalR (push)   │   Service    │
│  (Browser)   │ ◄═══════════════►  │  (HTTP mode) │
│              │    REST POST (cmd)  │              │
│              │ ──────────────────► │              │
└──────────────┘                     └──────────────┘
```

| Channel | Purpose | Direction |
|---|---|---|
| **OData** | Metrics, Analytics, Orchestrations queries | Client → Server (request/response) |
| **SignalR** | Trigger completions, agent chat streaming | Bidirectional (push + send) |
| **REST POST** | Trigger agent runs (`/agents/shield/perimeter-audit`) | Client → Server |

### 3.1 SignalR Hub contract

```csharp
public interface IOpenDefenderHub
{
    // Server → Client
    Task TriggerCompleted(string triggerName, string level, string detail);
    Task AgentChatToken(string agentName, string token);
    Task AgentChatDone(string agentName, string fullResponse);

    // Client → Server
    Task RunAgent(string agentName, string triggerName);
    Task SendChatMessage(string agentName, string message);
}
```

### 3.2 SSE fallback (optional, for thin clients)

For clients that can't use SignalR (curl, embedded, CI):

```
GET /events → text/event-stream
  event: trigger_completed
  data: {"name":"daily_backup_check","level":"OK","detail":"..."}

  event: trigger_completed
  data: {"name":"daily_resource_check","level":"Warn","detail":"..."}
```

---

## 4) Technology Stack

| Layer | Choice | Rationale |
|---|---|---|
| Framework | Blazor (Server or WASM) or SvelteKit | Blazor = C# everywhere; SvelteKit = lighter, better DX |
| Real-time | SignalR | Already in ASP.NET Core, typed hub, auto-reconnect |
| Charts | Chart.js or Lightweight Charts | Orchestration timeline, resource gauges |
| Data grid | AG Grid or TanStack Table | OData metric explorer needs filtering/sorting |
| Styling | Tailwind CSS | Dark theme, utility-first, matches the mockup aesthetic |
| State | Reactive stores / Blazor cascading params | Keep it simple — no Redux |

---

## 5) What Changes from the Mockup

| Mockup Element | Problem | Replacement |
|---|---|---|
| "Login Risks / Device Posture / Network Alerts / Vulnerabilities" KPIs | Generic SOC metrics, not ours | **Shield / Anchor / Core / Ledger** agent health cards |
| "Alert Feed" | We don't have alerts, we have trigger results | **Recent Trigger Activity** feed |
| "Incident Graph" | No incident correlation model | **Orchestration Timeline** (run history) |
| "Active Sessions" | We don't track user sessions | **Scheduler Status** (trigger schedule) |
| "Security Health" score | Single-domain score | **Per-agent health** derived from Analytics |
| "Alerts & Chat" sidebar | Right concept, wrong data | **Agent Chat** — talk to Shield/Anchor/Core/Ledger |

---

## 6) Implementation Phases

### Phase 1 — Read-only dashboard
- Overview page with agent KPI cards
- OData metric explorer
- Scheduler status
- No SignalR, just polling

### Phase 2 — Real-time + agent control
- SignalR push for trigger completions
- "Run Now" buttons on agent pages
- Live resource gauges

### Phase 3 — Agent chat
- Chat sidebar with agent selection
- Streaming responses via SignalR
- Chat history (persist to SQLite or ephemeral)

### Phase 4 — Multi-user
- Auth (cookie or JWT)
- Role-based views
- Audit log
