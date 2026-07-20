using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Tcp;

/// <summary>Registration helper for <see cref="TcpHealthCheck"/>.</summary>
public static class Extensions
{
    /// <summary>Registers a <see cref="TcpHealthCheck"/> that connects to <paramref name="host"/>:<paramref name="port"/>.</summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="host">The host to connect to.</param>
    /// <param name="port">The TCP port to connect to.</param>
    public static IHealthCheckBuilder AddTcpPing(this IHealthCheckBuilder builder, string host, int port)
    {
        return builder.AddHealthCheckFactory(new TcpHealthCheckFactory(host, port));
    }
}
