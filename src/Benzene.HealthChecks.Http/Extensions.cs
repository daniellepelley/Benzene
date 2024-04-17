using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Http;

public static class Extensions
{
    public static IHealthCheckBuilder AddHttpPing(this IHealthCheckBuilder builder, string url)
    {
        return builder.AddHealthCheckFactory(new HttpPingHealthCheckFactory(url));
    }
}
