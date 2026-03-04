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
/// Ledger Agent: Logging and telemetry analyst.
/// </summary>
public sealed class LedgerAgent : BaseAgent
{
    private static readonly List<AITool> _tools = LoggingTools.GetTools().ToList();

    protected override string SystemPrompt => _systemPrompt;
    protected override IEnumerable<AITool> DefaultTools => _tools;

    private const string _systemPrompt = @"<identity>
You are Ledger, the logging and telemetry analyst for this system.
You observe the evidence pipeline: log completeness, retention compliance, shipping health, and coverage gaps.
Your job is to read data, assess risk, and report findings. You do not make changes to the system.
</identity>

<scope>
You are responsible for:
- System journal health and retention
- Legacy log management and rotation
- Application-specific log collection
- Log shipping and centralization
- Telemetry pipeline integrity (ingestion, backlog, drops)
- Coverage: ensuring all critical systems are producing observable output
- Noise management: filtering high-volume/low-value logs before storage

You are NOT responsible for:
- Firewall, IAM, SSH, or network perimeter (that is Shield)
- Backup chain, service restart policies, or configuration drift (that is Anchor)
- Hardware health, kernel tuning, or GPU config (that is Core)
</scope>

<data_access>
You have access to the following data through the observability API.

Primary (you own these):
- LoggingMetrics: Journal disk usage, pipeline component health
  Properties: Timestamp, Component, Metric, Value (double), Status
- LoggingInventoryMetrics: Log source inventory, types, sizes
  Properties: Timestamp, LogSource, LogType, SizeBytes (long), Status

Analytics (pre-computed from raw data):
- LedgerAnalytics: Growth trends, retention compliance, coverage gap flags
  Properties: Timestamp, LogSource, LogType, CurrentSizeBytes (long), GrowthBytes (long), GrowthRateBytesPerHour (double), RetentionDays (double), IsRetentionCompliant (bool), GapDetected (bool), ShippingBacklog (double), IsBacklogBreach (bool)

Cross-reference (read-only, you do not own):
- ControlMapMetrics: Overall system layer status (check General Logging row)

Refer to the tool_spec for how to query these data sources.

Reference documentation (your knowledge base):
- logging.md: Journal retention, log rotation, forwarding and centralization
- logging_specifics.md: Deep dive on journal vs syslog, application logs, shipping patterns, rotation/compression

Control map layers you are authoritative for (from control_map.md):
- Layer 7: General Logging
</data_access>

<capabilities>
You can read:
- Journal disk usage
- Boot history
- Kernel logs from previous boots
- Logs by time range or count
- Log rotation configuration and status
- Legacy log directories and file sizes
- Total log storage consumption
- Telemetry shipper health and status
- Log source inventory

You cannot vacuum journals, force rotation, modify retention, or change any system state. You read and report only.

Refer to the tool_spec for the specific read tools available in the current environment.
</capabilities>

<constraints>
- If auth/audit logs are at risk of loss: escalate immediately as S1 Flag.
</constraints>
";
    public LedgerAgent(IChatClient client) : base(client)
    {
    }

    [TaskTrigger("daily_pipeline_check", "Check journal health, log source presence, and pipeline integrity.", "1.00:00:00")]
    public async Task<PipelineCheckResult> DailyPipelineCheck()
    {
        var response = await RunAsync<PipelineCheckResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Perform the daily pipeline check.

                1. Query LoggingMetrics. Check journal disk usage and pipeline component status. Flag any component not in a healthy state.
                2. Query LoggingInventoryMetrics. Confirm all expected log sources are present. Flag any missing source.
                3. Check for ingestion backlog or dropped events.

                Populate the result fields:
                - PipelineHealthy: true if all pipeline components are healthy with no drops or backlog.
                - MissingSourceCount: number of expected log sources that are absent or silent.
                - BacklogDetected: true if an ingestion backlog or dropped events were detected.
                - Flags: one entry per finding — include severity (S1–S4), evidence, and recommendation. A completely silent source is S2.
                - Detail: concise narrative summarizing pipeline and journal status.
                - Level: Info if pipeline is fully healthy. Warn if minor backlog. Error if sources are missing or components unhealthy. Critical if auth/audit logs are at risk of loss.
                """) });
        return response.Value ?? new PipelineCheckResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }

    [TaskTrigger("weekly_retention_audit", "Verify log retention meets policy targets per stream.", "7.00:00:00")]
    public async Task<RetentionAuditResult> WeeklyRetentionAudit()
    {
        var response = await RunAsync<RetentionAuditResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Perform the weekly retention audit.

                1. Query LoggingMetrics for journal disk usage.
                2. Determine how many days of history are available per stream:
                   - Auth/audit logs: target 180 days.
                   - Performance metrics: target 30 days.
                   - Application logs: note actual retention.
                3. Flag any stream below its target.
                4. Check rotation health: are rotation cycles on schedule? Any gaps?
                5. Identify high-noise log sources consuming disproportionate storage.

                Populate the result fields:
                - AllStreamsCompliant: true if every stream meets its retention policy target.
                - NonCompliantStreams: one entry per stream below target — include stream name, days available, and policy target.
                - HighNoiseSources: log sources consuming disproportionate storage.
                - Detail: concise narrative summarizing retention posture and rotation health.
                - Level: Info if all compliant. Warn if minor gaps in non-critical streams. Error if a critical stream is below target. Critical if auth/audit logs are at risk of loss.
                """) });
        return response.Value ?? new RetentionAuditResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }

    [TaskTrigger("weekly_health_brief", "Compile the Ledger weekly health brief.", "7.00:00:00")]
    public async Task<HealthBriefResult> WeeklyHealthBrief()
    {
        var response = await RunAsync<HealthBriefResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Compile the Ledger weekly health brief.

                Query LoggingMetrics, LoggingInventoryMetrics, and LedgerAnalytics for the past 7 days.

                1. Assess data loss risk: any logs dropped, shipper failures, or pipeline outages this week?
                2. Check retention status per stream and identify coverage gaps.
                3. Report pipeline health: journal size, ingestion backlog, shipper status.
                4. Keep each section to 3 items maximum.

                Populate the result fields:
                - TopRisks: up to 3 entries — greatest risks to logging completeness and integrity.
                - Changes: notable pipeline, retention, or coverage events this week.
                - OpenItems: unresolved coverage gaps or retention non-compliance.
                - Detail: concise narrative overview of the week's logging posture.
                - Level: Info if pipeline healthy and compliant. Warn if minor gaps. Error if data loss risk or non-compliant retention. Critical if auth/audit logs at risk.
                """) });
        return response.Value ?? new HealthBriefResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }
}
