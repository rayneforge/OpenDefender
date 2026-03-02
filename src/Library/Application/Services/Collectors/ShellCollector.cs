using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Domain.Abstractions;

namespace Library.Application.Services.Collectors;

/// <summary>
/// Base collector that executes a shell command and parses stdout into metric models.
/// Subclasses define platform-specific commands and shared parsing logic.
/// Linux commands run via /bin/bash. Windows commands run via powershell.exe (EncodedCommand).
/// </summary>
public abstract class ShellCollector<T> : ICollector<T>
{
    /// <summary>
    /// Routes to the platform-specific command builder.
    /// </summary>
    protected string BuildCommand(DateTime? since)
        => OperatingSystem.IsWindows() ? BuildWindowsCommand(since) : BuildLinuxCommand(since);

    /// <summary>
    /// Build the Linux (bash) shell command string.
    /// </summary>
    protected abstract string BuildLinuxCommand(DateTime? since);

    /// <summary>
    /// Build the Windows (PowerShell) command string.
    /// Override in subclasses to add Windows support.
    /// </summary>
    protected virtual string BuildWindowsCommand(DateTime? since)
        => throw new PlatformNotSupportedException($"{GetType().Name} does not support Windows.");

    /// <summary>
    /// Parse the raw stdout lines into model instances. Shared across platforms
    /// when both commands produce the same output format.
    /// </summary>
    protected abstract IEnumerable<T> Parse(IReadOnlyList<string> lines, DateTime collectedAt);

    public async Task<IEnumerable<T>> CollectAsync(DateTime? since = null, CancellationToken ct = default)
    {
        var command = BuildCommand(since);

        var output = await RunShellAsync(command, ct);
        var collectedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(output))
            return Enumerable.Empty<T>();

        var lines = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        return Parse(lines, collectedAt);
    }

    /// <summary>
    /// Executes a platform-appropriate shell command and returns stdout.
    /// Linux: /bin/bash -c "command"
    /// Windows: powershell.exe -EncodedCommand (Base64)
    /// </summary>
    protected static async Task<string> RunShellAsync(string command, CancellationToken ct = default)
    {
        ProcessStartInfo psi;

        if (OperatingSystem.IsWindows())
        {
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));
            psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -EncodedCommand {encoded}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        using var process = new Process { StartInfo = psi };
        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return stdout;
    }
}
