using Xunit;
using Library.Application.Services.Collectors;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Commands;

/// <summary>
/// Tests that each collector's Windows (PowerShell) commands execute without
/// error and return parseable results on a Windows host. Commands needing
/// elevated privileges or specific hardware are tested for no-throw only.
/// </summary>
public class WindowsCommandTests
{
    // ── Resource (Get-CimInstance Win32_Processor / Win32_OperatingSystem) ──
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task ResourceCollector_ReturnsMetrics()
    {
        var collector = new ResourceCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Metric == "CPU_Usage");
        Assert.Contains(results, r => r.Metric == "Memory_Usage");
        Assert.Contains(results, r => r.Metric == "Load_Avg_1m");
    }

    // ── Kernel (Environment.OSVersion, Registry UAC, CIM boot time) ────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task KernelCollector_ReturnsMetrics()
    {
        var collector = new KernelCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Category == "Kernel" && r.Metric == "Version");
        Assert.Contains(results, r => r.Category == "Boot" && r.Metric == "TotalTime");
    }

    // ── Networking (Get-NetAdapterStatistics) ──────────────────────────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
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

    // ── Logging (EventLog disk usage + count) ──────────────────────────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task LoggingCollector_ReturnsMetrics()
    {
        var collector = new LoggingCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Component == "EventLog");
    }

    // ── Logging Inventory (winevt\Logs\*.evtx) ────────────────────────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task LoggingInventoryCollector_ReturnsMetrics()
    {
        var collector = new LoggingInventoryCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r.SizeBytes >= 0));
    }

    // ── Automation (Get-ScheduledTask) ─────────────────────────────────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task AutomationCollector_ReturnsMetrics()
    {
        var collector = new AutomationCollector();
        var results = (await collector.CollectAsync()).ToList();

        // Windows always has scheduled tasks
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal("ScheduledTask", r.Tool));
    }

    // ── Data Recovery (Get-PSDrive) ────────────────────────────────────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
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

    // ── Service (Get-Service) ──────────────────────────────────────────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task ServiceCollector_DoesNotThrow()
    {
        var collector = new ServiceCollector("sshd", "W3SVC", "WinRM");
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotNull(results);
        Assert.All(results, r => Assert.False(string.IsNullOrWhiteSpace(r.Service)));
    }

    // ── Security (NetFirewall, NetTCPConnection, Security EventLog) ────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task SecurityCollector_DoesNotThrow()
    {
        var collector = new SecurityCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotNull(results);
    }

    // ── Hardware (PhysicalDisk, ThermalZone WMI) ───────────────────────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task HardwareCollector_DoesNotThrow()
    {
        var collector = new HardwareCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotNull(results);
    }

    // ── GPU (nvidia-smi + Win32_VideoController) ───────────────────────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task GpuCollector_ReturnsMetrics()
    {
        var collector = new GpuCollector();
        var results = (await collector.CollectAsync()).ToList();

        // Win32_VideoController should always return at least one GPU
        Assert.NotEmpty(results);
        Assert.All(results, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Vendor));
            Assert.False(string.IsNullOrWhiteSpace(r.Device));
        });
    }

    // ── Packet Tracing (Get-NetTCPConnection Established count) ────────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task PacketTracingCollector_DoesNotThrow()
    {
        var collector = new PacketTracingCollector(captureSeconds: 1);
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotNull(results);
    }

    // ── Control Map (composite — OS version, firewall, tasks) ──────────────
    [Fact]
    [Trait("Category", "WindowsCommand")]
    public async Task ControlMapCollector_ReturnsMetrics()
    {
        var collector = new ControlMapCollector();
        var results = (await collector.CollectAsync()).ToList();

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.Layer == "Kernel");
    }
}
