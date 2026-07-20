using System.Net.Sockets;
using System.Threading;
using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Tcp;

/// <summary>
/// Verifies a dependency is reachable at the L4 (TCP) level by opening a connection to a host and port -
/// the lowest-common-denominator check for anything without a first-class client (a database port, an
/// SMTP server, a custom service). Healthy if the connection is accepted; unhealthy on any socket error.
/// </summary>
public class TcpHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly int _port;
    private readonly ICancellationTokenAccessor? _cancellation;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="host">The host to connect to.</param>
    /// <param name="port">The TCP port to connect to.</param>
    /// <param name="cancellation">Supplies the ambient cancellation token for the connect; null observes no cancellation.</param>
    public TcpHealthCheck(string host, int port, ICancellationTokenAccessor? cancellation = null)
    {
        _host = host;
        _port = port;
        _cancellation = cancellation;
    }

    /// <inheritdoc />
    public string Type => "Tcp";

    /// <inheritdoc />
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("Tcp", $"{_host}:{_port}") };
        var token = _cancellation?.CancellationToken ?? CancellationToken.None;

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port, token);

            return HealthCheckResult.CreateInstance(true, Type,
                new Dictionary<string, object> { { "Host", _host }, { "Port", _port } }, dependencies);
        }
        catch (Exception ex)
        {
            // Report the failure type, not the message (a message can carry infra detail); an expected
            // "connection refused" is a failed result, not a thrown exception.
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object> { { "Host", _host }, { "Port", _port }, { "Error", ex.GetType().Name } },
                dependencies);
        }
    }
}
