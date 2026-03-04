using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Library.Domain.Abstractions;
using Library.Domain.Models.Evaluation;
using Library.Domain.Models.Results;
using Library.Domain.Attributes;
using Library.Application.Tooling;

namespace Library.Application.Agents.Delegates;

/// <summary>
/// Anchor Agent: Reliability and recovery analyst.
/// </summary>
public sealed class AnchorAgent : BaseAgent
{
    private static readonly List<AITool> _tools = ReliabilityTools.GetTools().ToList();

    protected override string SystemPrompt => _systemPrompt;
    protected override IEnumerable<AITool> DefaultTools => _tools;

    private const string _systemPrompt = @"<identity>
You are Anchor, the reliability and recovery analyst for this system.
You observe the continuity posture: backup health, service stability, configuration drift, and disaster recovery readiness.
Your job is to read data, assess risk, and report findings. You do not make changes to the system.
</identity>

<scope>
You assess the following domains:
- Backup chain integrity and snapshot health
- Encryption at rest status
- Service lifecycle stability
- Configuration drift from desired state
- Scheduled job health
- Disaster recovery readiness

Outside your scope (other roles own these):
- Firewall rules, IAM, SSH hardening, network perimeter (Shield)
- Kernel parameters, hardware tuning, CPU governors, GPU config (Core)
- Log retention, rotation, telemetry shipping (Ledger)
</scope>

<data_access>
You have access to the following data through the observability API.

Primary (you own these):
- DataRecoveryMetrics: Mount state, backup target availability
  Properties: Timestamp, Source, Status, SizeBytes (long)
- AutomationMetrics: Timer/job health, active automation count
  Properties: Timestamp, Tool, Job, Result
- ServiceMetrics: Service lifecycle state
  Properties: Timestamp, Service, Status, UptimeSeconds (double)

Analytics (pre-computed from raw data):
- ReliabilityAnalytics: Degradation detection, restart flags, gap detection
  Properties: Timestamp, Scope, Entity, StatusChange, IsDegraded (bool), GapDetected (bool)

Cross-reference (read-only, you do not own):
- ControlMapMetrics: Overall system layer status (check Automation row)
- HardwareMetrics: Disk health warnings that may affect backup targets

Refer to the tool_spec for how to query these data sources.

Reference documentation (your knowledge base):
- data_recovery.md: Encryption at rest, backup tools, snapshot strategies, sync/copy operations
- automation.md: Configuration management playbooks, scheduled jobs, infrastructure as code
- services_apps.md: Service unit management, restart policies, drift detection

Control map layers you are authoritative for (from control_map.md):
- Layer 4: Services/Applications
- Layer 6: Data/Recovery
- Layer 9: Automation & Orchestration
</data_access>

<capabilities>
You can read:
- Filesystem layout and mount state
- Backup snapshot lists and timestamps
- Encryption status of volumes
- Filesystem-level snapshot history
- Backup target sizes and capacity
- Encryption key inventory
- Active scheduled jobs and timers
- Automation tooling version and reachability
- Configuration drift reports (dry-run output)
- Scheduled user-level jobs
- Uncommitted configuration changes
- Service status, uptime, and logs
- Boot-enabled service list
- Service dependency chains
- Process group listings

You cannot restart services, roll back, modify playbooks, or change any system state. You read and report only.

Refer to the tool_spec for the specific read tools available in the current environment.
</capabilities>

<constraints>
- When RPO or RTO is at risk: escalate immediately as a Flag with recommended action.
- Always recommend specific actions in your Flags — the owner or another process will execute them.
</constraints>
";

    public AnchorAgent(IChatClient client) : base(client)
    {
    }

    [TaskTrigger("daily_backup_check", "Verify backup chain integrity, snapshot recency, and encryption status.", "1.00:00:00")]
    public async Task<BackupCheckResult> DailyBackupCheck()
    {
        var response = await RunAsync<BackupCheckResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Perform the daily backup check.

                1. Query DataRecoveryMetrics for the latest backup entries. Confirm all expected sources have a recent snapshot within the last 24 hours.
                2. For each source: check Status. If any backup failed or is missing, flag it.
                3. Check SizeBytes for significant deviation from the previous run (>20% change). Flag anomalies.
                4. Verify encryption-at-rest status if available in the data.

                Populate the result fields:
                - AllBackupsCurrent: true if every expected source has a backup within the RPO window.
                - EncryptionVerified: true if encryption-at-rest is confirmed on backup targets.
                - FailedSourceCount: number of sources with a missing or failed backup.
                - Flags: one entry per failing source or anomaly — include severity (S1–S4), evidence, and recommendation.
                - Detail: concise narrative summarizing backup chain status.
                - Level: Info if all backups current and encrypted. Warn if minor size anomalies. Error if a source is missing or failed. Critical if a critical source has no backup or RPO is breached.
                """) });
        return response.Value ?? new BackupCheckResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }

    [TaskTrigger("daily_service_stability", "Check for failed or unstable services and timer health.", "1.00:00:00")]
    public async Task<ServiceStabilityResult> DailyServiceStability()
    {
        var response = await RunAsync<ServiceStabilityResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Perform the daily service stability check.

                1. Query ServiceMetrics. Identify any service with a failed or inactive status that should be running.
                2. For each affected service: check uptime. If a service has restarted more than 3 times in 24 hours, classify as systemic.
                3. Query AutomationMetrics. Are scheduled timers healthy? Is the active count at the expected level?

                Populate the result fields:
                - AllServicesStable: true if all monitored services are running without failures.
                - FailedServiceCount: number of services in a failed or inactive state.
                - TimersHealthy: true if scheduled timers and automation jobs are at expected counts.
                - Flags: one entry per systemic failure — include severity (S1–S4), evidence, and recommendation. One-off failures: note them in Detail only.
                - Detail: concise narrative summarizing service and timer health.
                - Level: Info if all stable. Warn if one-off failures observed. Error if systemic failures detected. Critical if a critical service is unresponsive.
                """) });
        return response.Value ?? new ServiceStabilityResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }

    [TaskTrigger("weekly_drift_check", "Detect configuration drift from desired state.", "7.00:00:00")]
    public async Task<DriftCheckResult> WeeklyDriftCheck()
    {
        var response = await RunAsync<DriftCheckResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Perform the weekly configuration drift check.

                1. Query AutomationMetrics. Compare timer counts and job results against the expected baseline.
                2. For each divergence: identify what changed and when. Determine if the change was authorized.

                Populate the result fields:
                - DriftDetected: true if any configuration divergence was found, false otherwise.
                - DriftItems: one entry per drift item — include expected value, actual value, authorization status, and severity.
                - Detail: concise narrative summarizing drift posture.
                - Level: Info if no drift. Warn if minor authorized drift only. Error if unauthorized drift detected. Critical if drift affects recovery capability.
                """) });
        return response.Value ?? new DriftCheckResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }

    [TaskTrigger("weekly_health_brief", "Compile the Anchor weekly health brief.", "7.00:00:00")]
    public async Task<HealthBriefResult> WeeklyHealthBrief()
    {
        var response = await RunAsync<HealthBriefResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Compile the Anchor weekly health brief.

                Query DataRecoveryMetrics, ServiceMetrics, AutomationMetrics, and ReliabilityAnalytics for the past 7 days.

                1. Compute backup success rate and RPO compliance.
                2. Identify the top 3 systems at recovery risk.
                3. Summarize automation drift and service stability.
                4. Keep each section to 3 items maximum.

                Populate the result fields:
                - TopRisks: up to 3 entries — systems at greatest recovery risk with severity and evidence.
                - Changes: notable backup, automation, and service events this week.
                - OpenItems: unresolved reliability issues or items flagged for remediation.
                - Detail: concise narrative overview of the week's reliability posture.
                - Level: Info if posture is stable. Warn if minor drift or one-off failures. Error if RPO at risk or systemic instability. Critical if data loss is imminent.
                """) });
        return response.Value ?? new HealthBriefResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }
}
