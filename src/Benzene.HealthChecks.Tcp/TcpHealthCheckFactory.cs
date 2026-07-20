using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Tcp;

/// <summary>Builds a <see cref="TcpHealthCheck"/> for a fixed host/port, resolving the ambient cancellation accessor from DI.</summary>
public class TcpHealthCheckFactory : IHealthCheckFactory
{
    private readonly string _host;
    private readonly int _port;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="host">The host the resulting health check will connect to.</param>
    /// <param name="port">The TCP port the resulting health check will connect to.</param>
    public TcpHealthCheckFactory(string host, int port)
    {
        _host = host;
        _port = port;
    }

    /// <inheritdoc />
    public IHealthCheck Create(IServiceResolver resolver)
    {
        return new TcpHealthCheck(_host, _port, resolver.TryGetService<ICancellationTokenAccessor>());
    }
}
