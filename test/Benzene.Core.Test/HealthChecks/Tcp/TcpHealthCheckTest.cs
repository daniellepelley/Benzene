using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Benzene.HealthChecks.Core;
using Benzene.HealthChecks.Tcp;
using Xunit;

namespace Benzene.Test.HealthChecks.Tcp;

public class TcpHealthCheckTest
{
    [Fact]
    public async Task ExecuteAsync_PortAccepting_ReturnsHealthy()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        try
        {
            var result = await new TcpHealthCheck("127.0.0.1", port).ExecuteAsync();

            Assert.Equal(HealthCheckStatus.Ok, result.Status);
            var dependency = Assert.Single(result.Dependencies);
            Assert.Equal("Tcp", dependency.Kind);
            Assert.Equal($"127.0.0.1:{port}", dependency.Name);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task ExecuteAsync_ConnectionRefused_ReturnsUnhealthy_WithTheDependency()
    {
        // Bind to grab a free port, then release it so nothing is listening -> connection refused.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var result = await new TcpHealthCheck("127.0.0.1", port).ExecuteAsync();

        Assert.Equal(HealthCheckStatus.Failed, result.Status);
        Assert.True(result.Data.ContainsKey("Error"));
        Assert.Equal($"127.0.0.1:{port}", Assert.Single(result.Dependencies).Name);
    }
}
