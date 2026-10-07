using System.Net;
using System.Net.Sockets;
using Library.Application.Services.Collectors;

namespace Tests.Commands;

public class NetworkConnectionTests
{
    [Theory]
    [Trait("Category", "NetworkConnection")]
    [InlineData("ESTAB 0 0 192.0.2.10:51000 198.51.100.20:443", "192.0.2.10", 51000, "198.51.100.20", 443, "Established")]
    [InlineData("tcp ESTAB 0 0 [2001:db8::1]:51000 [2001:db8::2]:443", "2001:db8::1", 51000, "2001:db8::2", 443, "Established")]
    [InlineData("LISTEN 0 128 [::]:22 [::]:*", "::", 22, "::", null, "Listen")]
    [InlineData("Established|127.0.0.1|50000|127.0.0.1|8080", "127.0.0.1", 50000, "127.0.0.1", 8080, "Established")]
    public void ParsesEndpoints(string raw, string local, int localPort, string remote, int? remotePort, string state)
    {
        var timestamp = DateTime.UtcNow;
        var connection = Assert.Single(NetworkConnectionCollector.ParseSnapshot([raw], timestamp));
        Assert.Equal(local, connection.LocalAddress);
        Assert.Equal(localPort, connection.LocalPort);
        Assert.Equal(remote, connection.RemoteAddress);
        Assert.Equal(remotePort, connection.RemotePort);
        Assert.Equal(state, connection.State);
        Assert.Equal(timestamp, connection.Timestamp);
    }

    [Fact]
    [Trait("Category", "NetworkConnection")]
    public void UnavailableDoesNotReturnAnEmptyHealthyResult()
        => Assert.Throws<InvalidOperationException>(() =>
            NetworkConnectionCollector.ParseSnapshot(["UNAVAILABLE"], DateTime.UtcNow));

    [Fact]
    [Trait("Category", "NetworkConnection")]
    public void RejectsMalformedOutput()
        => Assert.Throws<FormatException>(() =>
            NetworkConnectionCollector.ParseSnapshot(["unexpected output"], DateTime.UtcNow));

    [Fact]
    [Trait("Category", "NetworkConnection")]
    public async Task SeesAControlledLoopbackConnection()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
        using var server = await listener.AcceptTcpClientAsync(timeout.Token);
        var connections = (await new NetworkConnectionCollector().CollectAsync(ct: timeout.Token)).ToList();
        Assert.Contains(connections, connection => connection.State == "Established"
            && connection.LocalAddress == "127.0.0.1" && connection.RemoteAddress == "127.0.0.1"
            && connection.RemotePort == port);
    }
}
