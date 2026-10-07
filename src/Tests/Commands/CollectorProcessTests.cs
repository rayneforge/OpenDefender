using Library.Application.Services.Collectors;

namespace Tests.Commands;

public class CollectorProcessTests
{
    [Fact]
    [Trait("Category", "Process")]
    public async Task LargeStderrDoesNotBlockStdout()
    {
        var command = OperatingSystem.IsWindows()
            ? "[Console]::Error.Write(('x' * 262144)); [Console]::Out.Write('ok')"
            : "head -c 262144 /dev/zero >&2; printf ok";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.Equal("ok", (await Probe.Execute(command, timeout.Token)).Trim());
    }

    [Fact]
    [Trait("Category", "Process")]
    public async Task CancellationStopsLongRunningProbe()
    {
        var command = OperatingSystem.IsWindows() ? "Start-Sleep -Seconds 20" : "sleep 20";
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        var task = Probe.Execute(command, cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    private sealed class Probe : ShellCollector<string>
    {
        public static Task<string> Execute(string command, CancellationToken ct) => RunShellAsync(command, ct);
        protected override string BuildLinuxCommand(DateTime? since) => "";
        protected override IEnumerable<string> Parse(IReadOnlyList<string> lines, DateTime collectedAt) => lines;
    }
}
