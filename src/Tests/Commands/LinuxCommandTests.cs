using Xunit;
using Library.Application.Services.Collectors;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Commands;

/// <summary>
/// Tests that each collector's Linux (bash) commands execute without error
/// and return parseable results on a Linux host. Commands needing sudo or
/// specific hardware (smartctl, tcpdump, nvidia-smi) are tested for
/// no-throw behaviour only since they may return empty on CI runners.
/// </summary>
public class LinuxCommandTests
{
    // ── Resource (top, free, uptime) ────────────────────────────────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task ResourceCollector_ReturnsMetrics()
    {
        var collector = new ResourceCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Metric == "CPU_Usage");
        Assert.Contains(results, r => r.Metric == "Memory_Usage");
        Assert.Contains(results, r => r.Metric == "Load_Avg_1m");
    }

    // ── Kernel (uname, sysctl) ──────────────────────────────────────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task KernelCollector_ReturnsMetrics()
    {
        var collector = new KernelCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Category == "Kernel" && r.Metric == "Version");
    }

    // ── Networking (ip -s link show) ────────────────────────────────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task NetworkingCollector_ReturnsMetrics()
    {
        var collector = new NetworkingCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotEmpty(results);
        Assert.All(results, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Interface));
            Assert.Contains(r.Metric, new[] { "RX_Bytes", "TX_Bytes" });
        });
    }

    // ── Logging (du /var/log/journal, journalctl --list-boots) ──────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task LoggingCollector_ReturnsMetrics()
    {
        var collector = new LoggingCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Component == "JournalD");
    }

    // ── Logging Inventory (find /var/log) ───────────────────────────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task LoggingInventoryCollector_ReturnsMetrics()
    {
        var collector = new LoggingInventoryCollector();
        var results = (await collector.CollectAsync()).ToList();

        // /var/log should always have at least one .log file
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.SizeBytes >= 0));
    }

    // ── Automation (systemctl list-timers) ──────────────────────────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task AutomationCollector_DoesNotThrow()
    {
        var collector = new AutomationCollector();
        var results = (await collector.CollectAsync()).ToList();

        // CI runners may have zero timers — just verify no crash
        Assert.NotNull(results);
    }

    // ── Data Recovery (df) ──────────────────────────────────────────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task DataRecoveryCollector_ReturnsMetrics()
    {
        var collector = new DataRecoveryCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotEmpty(results);
        Assert.All(results, r =>
        {
            Assert.Equal("Mounted", r.Status);
            Assert.True(r.SizeBytes >= 0);
        });
    }

    // ── Service (systemctl is-active) ───────────────────────────────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task ServiceCollector_DoesNotThrow()
    {
        var collector = new ServiceCollector("sshd", "docker");
        var results = (await collector.CollectAsync()).ToList();

        // Services may not be installed — verify no crash and correct parse
        Assert.NotNull(results);
        Assert.All(results, r => Assert.False(string.IsNullOrWhiteSpace(r.Service)));
    }

    // ── Security (sudo ufw, ss, journalctl — may need elevated) ────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task SecurityCollector_DoesNotThrow()
    {
        var collector = new SecurityCollector();
        var results = (await collector.CollectAsync()).ToList();

        // sudo commands may return empty on CI — just verify no crash
        Assert.NotNull(results);
    }

    // ── Hardware (smartctl, sensors — may need hardware/sudo) ───────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task HardwareCollector_DoesNotThrow()
    {
        var collector = new HardwareCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotNull(results);
    }

    // ── GPU (nvidia-smi, rocm-smi, /sys/class/drm) ─────────────────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task GpuCollector_DoesNotThrow()
    {
        var collector = new GpuCollector();
        var results = (await collector.CollectAsync()).ToList();

        // No GPU on CI is expected — verify no crash
        Assert.NotNull(results);
    }

    // ── Packet Tracing (tcpdump — needs sudo) ──────────────────────
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task PacketTracingCollector_DoesNotThrow()
    {
        var collector = new PacketTracingCollector(captureSeconds: 1);
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotNull(results);
    }

    // ── Control Map (composite — sensors, uname, ss, nvidia-smi) ───
    [Fact]
    [Trait("Category", "LinuxCommand")]
    public async Task ControlMapCollector_DoesNotThrow()
    {
        var collector = new ControlMapCollector();
        var results = (await collector.CollectAsync()).ToList();

        // At least Kernel should always succeed
        Assert.NotNull(results);
    }
}
