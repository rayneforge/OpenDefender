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
/// Shield Agent: Security and connectivity analyst.
/// </summary>
public sealed class ShieldAgent : BaseAgent
{
    private static readonly List<AITool> _tools = SecurityTools.GetTools().ToList();

    protected override string SystemPrompt => _systemPrompt;
    protected override IEnumerable<AITool> DefaultTools => _tools;

    private const string _systemPrompt = @"<identity>
You are Shield, the security and connectivity analyst for this system.
You observe the defensive perimeter: access control, network integrity, and traffic safety.
Your job is to read data, assess risk, and report findings. You do not make changes to the system.
</identity>

<scope>
You assess the following domains:
- Firewall state and rule posture
- Authentication and access control health
- Mandatory access control profile status
- Network interface configuration, DNS, and routing integrity
- Packet capture and traffic patterns
- File integrity baselines
- Vulnerability posture

Outside your scope (other roles own these):
- Service lifecycle and restart policies (Anchor)
- Kernel parameters, hardware tuning, GPU config (Core)
- Log retention, rotation policies, telemetry shipping (Ledger)
</scope>

<data_access>
You have access to the following data through the observability API.

Primary (you own these):
- SecurityChecks: Firewall status, open port count, severity flags
  Properties: Timestamp, CheckType, Item, Result, Value (double), Severity
- NetworkingMetrics: Interface IPs, link state, traffic counters
  Properties: Timestamp, Interface, Metric, Value (double), Status
- PacketTracingMetrics: Active captures, anomaly indicators
  Properties: Timestamp, Interface, PacketsCaptured (int), Status

Analytics (pre-computed from raw data):
- SecurityAnalytics: Breach flags, new issue counts
  Properties: Timestamp, CheckType, NewIssuesCount (int), IsBreach (bool), Severity

Cross-reference (read-only, you do not own):
- ControlMapMetrics: Overall system layer status (check the Security row and ActionRequired field)

Refer to the tool_spec for how to query these data sources.

Reference documentation (your knowledge base):
- security.md: Firewall rules, access controls, hardening, integrity monitoring, vulnerability scanning
- networking.md: Interface config, routing, DNS, network stack tuning
- packet_tracing.md: Packet capture, traffic analysis, kernel-level tracing, filtering

Control map layers you are authoritative for (from control_map.md):
- Layer 5: Security
- Layer 8: Networking & Connectivity
- Layer 10: Packet Tracing & Traffic Analysis
</data_access>

<capabilities>
You can read:
- Firewall state and current rules
- Open ports and listening services
- Security audit results
- File integrity check results
- Authentication history and failed access attempts
- Mandatory access control profile status
- Network interface state, routing tables, and DNS resolution
- Network path diagnostic results
- Bandwidth usage
- Packet capture output
- Active connection traces
- Per-process bandwidth consumption

You cannot write, modify, block, restart, or change any system state. You read and report only.

Refer to the tool_spec for the specific read tools available in the current environment.
</capabilities>

<constraints>
- Default stance: zero-trust. Assume new connections and access patterns are suspicious until verified.
- If anything looks like exfiltration or active compromise: escalate immediately as S1 Flag with recommended action. Do not wait.
- If a finding is ambiguous: report it with your confidence level. Let the owner decide.
</constraints>
";
    public ShieldAgent(IChatClient client) : base(client)
    {
    }

    [TaskTrigger("daily_perimeter_audit", "Check firewall, auth failures, interface state, and packet anomalies.", "1.00:00:00")]
    public async Task<PerimeterAuditResult> DailyPerimeterAudit()
    {
        var response = await RunAsync<PerimeterAuditResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Perform the daily perimeter audit.

                1. Query SecurityChecks for the latest firewall status. Confirm the firewall is active. Count open ports and flag any new listeners not seen in the previous run.
                2. Query SecurityChecks for authentication data. Look for failed SSH logins in the last 24 hours. If any source IP has more than 5 failures, flag it.
                3. Query NetworkingMetrics for interface status. Confirm all expected interfaces are UP. Flag any interface that is DOWN or missing.
                4. Query PacketTracingMetrics. If any capture shows an anomaly status, flag it.

                Populate the result fields:
                - FirewallActive: true if the firewall is confirmed running.
                - OpenPortCount: total number of open ports observed.
                - AuthFailureCount: total authentication failures in the last 24 hours.
                - InterfaceIssues: true if any expected interface is DOWN or missing.
                - PacketAnomalies: true if any capture reported anomaly status.
                - Flags: one entry per finding — include severity (S1–S4), evidence, and recommendation.
                - Detail: concise narrative summarizing the overall audit outcome.
                - Level: Info if no issues. Warn if minor anomalies. Error if a defensive component is unhealthy. Critical if active compromise is suspected.
                """) });
        return response.Value ?? new PerimeterAuditResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }

    [TaskTrigger("weekly_policy_drift", "Detect drift in security policies against baseline.", "7.00:00:00")]
    public async Task<PolicyDriftResult> WeeklyPolicyDrift()
    {
        var response = await RunAsync<PolicyDriftResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Perform the weekly policy drift check.

                1. Query SecurityChecks for access-control profile data. Flag any profile in complain mode that should be enforced, or any missing profile.
                2. Query SecurityChecks for IAM-related checks. Flag new users, modified sudo rules, or loosened SSH configuration.
                3. Query NetworkingMetrics for current listeners. Compare against previous baseline. Flag new listeners.

                Populate the result fields:
                - DriftDetected: true if any policy divergence was found, false otherwise.
                - DriftFindings: one entry per drift item — include the policy, what changed, severity, and evidence.
                - Detail: concise narrative summarizing the drift posture.
                - Level: Info if no drift. Warn if minor drift (e.g., complain-mode profile). Error if security controls weakened. Critical if an enforced control was disabled or bypassed.
                """) });
        return response.Value ?? new PolicyDriftResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }

    [TaskTrigger("weekly_health_brief", "Compile the Shield weekly health brief.", "7.00:00:00")]
    public async Task<HealthBriefResult> WeeklyHealthBrief()
    {
        var response = await RunAsync<HealthBriefResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Compile the Shield weekly health brief.

                Query SecurityChecks, NetworkingMetrics, PacketTracingMetrics, and SecurityAnalytics for the past 7 days.

                1. Identify the top 3 risks by severity, then recency.
                2. List all changes this week: firewall rule changes, IAM modifications, patches applied, key rotations.
                3. List any open exposures or watch-list items not yet resolved.
                4. Keep each section to 3 items maximum.

                Populate the result fields:
                - TopRisks: up to 3 entries, each describing a risk with severity and evidence.
                - Changes: notable changes observed this week.
                - OpenItems: unresolved exposures or items trending toward risk.
                - Detail: concise narrative overview of the week's security posture.
                - Level: Info if posture is stable. Warn if minor issues are trending. Error if unresolved risks remain. Critical if active threats are present.
                """) });
        return response.Value ?? new HealthBriefResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }
}
