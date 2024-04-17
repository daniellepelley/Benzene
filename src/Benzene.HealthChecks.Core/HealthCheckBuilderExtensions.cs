namespace Benzene.HealthChecks.Core;

public static class HealthCheckBuilderExtensions
{
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, IHealthCheck healthCheck)
    {
        return source.AddHealthCheck(_ => healthCheck);
    }

    public static IHealthCheckBuilder AddHealthChecks(this IHealthCheckBuilder source, params IHealthCheck[] healthChecks)
    {
        foreach (var healthCheck in healthChecks)
        {
            source.AddHealthCheck(_ => healthCheck);
        }

        return source;
    }

    public static IHealthCheckBuilder AddHealthCheckFactory(this IHealthCheckBuilder source, IHealthCheckFactory healthCheckFactory)
    {
        return source.AddHealthCheck(healthCheckFactory.Create);
    }
}
