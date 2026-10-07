using System.Globalization;
using Library.Domain.Models.Metrics;

namespace Library.Application.Services.Collectors;

/// <summary>Reads current TCP socket endpoints with existing user permissions, without packet capture.</summary>
public sealed class NetworkConnectionCollector : ShellCollector<NetworkConnectionMetric>
{
    protected override string BuildLinuxCommand(DateTime? since) =>
        "if command -v ss >/dev/null 2>&1; then ss -H -n -t -a || echo UNAVAILABLE; else echo UNAVAILABLE; fi";

    protected override string BuildWindowsCommand(DateTime? since) => """
        try {
            Get-NetTCPConnection -ErrorAction Stop | ForEach-Object {
                Write-Output "$($_.State)|$($_.LocalAddress)|$($_.LocalPort)|$($_.RemoteAddress)|$($_.RemotePort)"
            }
        } catch { Write-Output 'UNAVAILABLE' }
        """;

    protected override IEnumerable<NetworkConnectionMetric> Parse(IReadOnlyList<string> lines, DateTime collectedAt)
        => ParseSnapshot(lines, collectedAt);

    public static IReadOnlyList<NetworkConnectionMetric> ParseSnapshot(IReadOnlyList<string> lines, DateTime collectedAt)
    {
        var result = new List<NetworkConnectionMetric>();
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line == "UNAVAILABLE")
                throw new InvalidOperationException("TCP connection inventory is unavailable. Check socket-query permissions and ss (Linux) or Get-NetTCPConnection (Windows). No elevation was attempted.");
            string state, localAddress, remoteAddress;
            int? localPort, remotePort;
            if (line.Contains('|'))
            {
                var fields = line.Split('|');
                if (fields.Length != 5) throw new FormatException("Unexpected TCP connection output.");
                state = fields[0];
                localAddress = fields[1];
                localPort = ParsePort(fields[2]);
                remoteAddress = fields[3];
                remotePort = ParsePort(fields[4]);
            }
            else
            {
                var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                // Single-protocol ss omits Netid; tolerate versions that include it.
                var offset = fields.Length == 6 && fields[0] == "tcp" ? 1 : 0;
                if (fields.Length != offset + 5) throw new FormatException("Unexpected ss TCP connection output.");
                state = fields[offset];
                (localAddress, localPort) = ParseEndpoint(fields[offset + 3]);
                (remoteAddress, remotePort) = ParseEndpoint(fields[offset + 4]);
            }
            state = state switch { "ESTAB" => "Established", "LISTEN" => "Listen", _ => state };
            result.Add(new NetworkConnectionMetric
            {
                Timestamp = collectedAt, State = state,
                LocalAddress = NormalizeAddress(localAddress), LocalPort = localPort,
                RemoteAddress = NormalizeAddress(remoteAddress), RemotePort = remotePort
            });
        }
        return result;
    }

    private static (string Address, int? Port) ParseEndpoint(string value)
    {
        var separator = value.LastIndexOf(':');
        if (separator < 0) throw new FormatException("Unexpected TCP endpoint format.");
        return (value[..separator].Trim('[', ']'), ParsePort(value[(separator + 1)..]));
    }

    private static int? ParsePort(string value) => value == "*" ? null
        : int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static string NormalizeAddress(string value)
        => System.Net.IPAddress.TryParse(value, out var address) && address.IsIPv4MappedToIPv6
            ? address.MapToIPv4().ToString() : value;
}
