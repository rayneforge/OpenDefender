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
/// Core Agent: Platform and workload analyst.
/// </summary>
public sealed class CoreAgent : BaseAgent
{
    private static readonly List<AITool> _tools = InfrastructureTools.GetTools().ToList();

    protected override string SystemPrompt => _systemPrompt;
    protected override IEnumerable<AITool> DefaultTools => _tools;

    private const string _systemPrompt = @"<identity>
You are Core, the platform and workload analyst for this system.
You observe the physical and logical foundation: hardware health, kernel/OS stability, resource performance, and specialized compute hardware.
Your job is to read data, assess risk, and report findings. You do not make changes to the system.
</identity>

<scope>
You are responsible for:
- Disk health and predictive failure monitoring
- Thermal management and fan monitoring
- BIOS/UEFI and firmware currency
- Power management and profiles
- Kernel version, parameters, modules, and boot performance
- CPU/Memory/IO utilization and resource isolation
- I/O scheduling and memory pressure management
- GPU and accelerator health

You are NOT responsible for:
- Firewall, IAM, SSH, security profiles, or network perimeter (that is Shield)
- Backup chain, service restart policies, or configuration drift (that is Anchor)
- Log retention, rotation, or telemetry shipping (that is Ledger)
</scope>

<data_access>
You have access to the following data through the observability API.

Primary (you own these):
- HardwareMetrics: Device health, temperature, SMART attributes
  Properties: Timestamp, Device, Attribute, Value (double), Status
- KernelMetrics: Kernel version, security params, boot time
  Properties: Timestamp, Category, Metric, Value (string — mixed content), Alert
- ResourceMetrics: CPU/Memory/Load utilization vs thresholds
  Properties: Timestamp, Metric, Value (double), Threshold (double)
- GpuMetrics: GPU utilization, memory, temperature
  Properties: Timestamp, Vendor, Device, GpuUtil (double), MemUtil (double), Temp (double)

Analytics (pre-computed from raw data):
- ResourceAnalytics: Deltas, rates, and breach flags
  Properties: Timestamp, Metric, CurrentValue (double), Delta (double), Rate (double), RateDelta (double), IsBreach (bool), Severity

Cross-reference (read-only, you do not own):
- ControlMapMetrics: Overall system layer status (check Hardware and Kernel rows)

Refer to the tool_spec for how to query these data sources.

Reference documentation (your knowledge base):
- hardware_firmware.md: Disk health, temperature sensors, BIOS/UEFI, power management, microcode
- kernel_os.md: Kernel parameters, modules, boot analysis, CPU governors, I/O schedulers
- resources_performance.md: Process monitoring, resource isolation, historical utilization, memory pressure
- gpu_accelerators.md: GPU monitoring per vendor, driver config, power limits

Control map layers you are authoritative for (from control_map.md):
- Layer 1: Hardware/Firmware
- Layer 2: Kernel/Operating System
- Layer 3: Resources/Performance
- (GPU/Accelerators as a sub-domain of Layers 1 and 3)
</data_access>

<capabilities>
You can read:
- Disk health reports and predictive failure indicators
- CPU, GPU, and ambient temperatures
- BIOS/firmware version
- Battery health and power profile status
- Available firmware updates
- Kernel version and loaded modules
- Kernel runtime parameters
- Boot performance breakdown
- Kernel-level errors and warnings
- Per-process CPU, memory, and IO usage
- Disk IO latency details
- Historical resource utilization
- Resource usage by control group
- GPU utilization, memory, and temperature per vendor
- Hardware-level graphics controller info
- 3D acceleration status

You cannot tune parameters, adjust priorities, apply updates, or change any system state. You read and report only.

Refer to the tool_spec for the specific read tools available in the current environment.
</capabilities>

<constraints>
- Owner-defined performance priorities you assess against:
  - Latency matters more than throughput.
  - Keep headroom above 30% on CPU, RAM, and disk.
- When headroom drops below 30% or a regression is detected: escalate as a Flag with recommended action.
</constraints>
";

    public CoreAgent(IChatClient client) : base(client)
    {
    }

    [TaskTrigger("daily_resource_check", "Check CPU, memory, disk, thermal, and GPU utilization against thresholds.", "1.00:00:00")]
    public async Task<ResourceCheckResult> DailyResourceCheck()
    {
        var response = await RunAsync<ResourceCheckResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Perform the daily resource check.

                1. Query ResourceMetrics. Check CPU, memory, disk, and swap utilization. Flag any value above 85%.
                2. Query KernelMetrics. Check boot time for deviation from baseline.
                3. Query HardwareMetrics. Check device temperatures and SMART status. Flag warnings.
                4. Query GpuMetrics. Check utilization and temperature. Flag thermal throttling.

                Populate the result fields:
                - WithinBaseline: true if all resources are within the 30% headroom threshold.
                - CpuPercent: observed CPU utilization percentage (0–100), or null if unavailable.
                - MemPercent: observed memory utilization percentage (0–100), or null if unavailable.
                - DiskPercent: observed disk utilization percentage (0–100), or null if unavailable.
                - ThermalAlert: true if any temperature sensor is above warning threshold.
                - Flags: one entry per threshold breach or anomaly — include severity (S1–S4), evidence, and recommendation.
                - Detail: concise narrative summarizing resource and hardware state.
                - Level: Info if all nominal. Warn if approaching thresholds (70–85%). Error if headroom breached (>85%). Critical if thermal throttling or SMART failure.
                """) });
        return response.Value ?? new ResourceCheckResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }

    [TaskTrigger("weekly_firmware_review", "Review kernel version, kptr_restrict, boot time, firmware updates, and disk health.", "7.00:00:00")]
    public async Task<FirmwareReviewResult> WeeklyFirmwareReview()
    {
        var response = await RunAsync<FirmwareReviewResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Perform the weekly firmware and kernel review.

                1. Query KernelMetrics. Note the current kernel version and KptrRestrict value. If KptrRestrict is 0, flag as S3.
                2. Check boot TotalTime for significant deviation from baseline.
                3. Query HardwareMetrics. Note device temperatures and SMART status.
                4. Note any pending firmware updates if available in the data.

                Populate the result fields:
                - KernelVersion: the current running kernel version string.
                - KptrRestrictCompliant: true if kptr_restrict is non-zero (compliant).
                - PendingFirmwareUpdates: true if firmware updates are available.
                - Flags: one entry per finding at S2 or above — include severity, evidence, and recommendation.
                - Detail: concise narrative covering kernel, boot, and firmware status.
                - Level: Info if all current and compliant. Warn if minor version lag or kptr_restrict non-compliant. Error if SMART warnings or critical firmware gap. Critical if disk failure imminent.
                """) });
        return response.Value ?? new FirmwareReviewResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }

    [TaskTrigger("weekly_health_brief", "Compile the Core weekly health brief.", "7.00:00:00")]
    public async Task<HealthBriefResult> WeeklyHealthBrief()
    {
        var response = await RunAsync<HealthBriefResult>(
            new[] { new ChatMessage(ChatRole.User, """
                Compile the Core weekly health brief.

                Query ResourceMetrics, GpuMetrics, HardwareMetrics, KernelMetrics, and ResourceAnalytics for the past 7 days.

                1. Report current headroom for CPU, RAM, Disk, and GPU.
                2. Note any regressions from baseline or adverse trends.
                3. List upcoming maintenance needs: firmware updates, kernel upgrades, scheduled reboots.
                4. Keep each section to 3 items maximum.

                Populate the result fields:
                - TopRisks: up to 3 entries — resources at greatest risk with severity and evidence.
                - Changes: notable resource, hardware, or kernel events this week.
                - OpenItems: pending maintenance or unresolved regressions.
                - Detail: concise narrative overview of the week's platform posture.
                - Level: Info if headroom is healthy. Warn if trending toward thresholds. Error if regressions observed. Critical if hardware failure risk.
                """) });
        return response.Value ?? new HealthBriefResult { Level = ResultLevel.Error, Detail = "Agent produced no response." };
    }
}
