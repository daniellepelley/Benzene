using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Http;

/// <summary>Registration helper for <see cref="HttpPingHealthCheck"/>.</summary>
public static class Extensions
{
    /// <summary>Registers an <see cref="HttpPingHealthCheck"/> that GETs <paramref name="url"/> and requires a 200 OK response to be considered healthy.</summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="url">The URL to ping.</param>
    public static IHealthCheckBuilder AddHttpPing(this IHealthCheckBuilder builder, string url)
    {
        return builder.AddHealthCheckFactory(new HttpPingHealthCheckFactory(url));
    }
}
