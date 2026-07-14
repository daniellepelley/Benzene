namespace Benzene.HealthChecks.Core;

/// <summary>
/// Convenience overloads layered on <see cref="IHealthCheckBuilder"/>'s two core registration
/// members, for registering an already-constructed instance or an <see cref="IHealthCheckFactory"/>
/// instead of a bare resolver function or a DI-resolved type.
/// </summary>
public static class HealthCheckBuilderExtensions
{
    /// <summary>Registers an already-constructed health check instance, reused on every run.</summary>
    /// <param name="source">The builder to register against.</param>
    /// <param name="healthCheck">The health check instance to run.</param>
    public static IHealthCheckBuilder AddHealthCheck(this IHealthCheckBuilder source, IHealthCheck healthCheck)
    {
        return source.AddHealthCheck(_ => healthCheck);
    }

    /// <summary>Registers multiple already-constructed health check instances, each reused on every run.</summary>
    /// <param name="source">The builder to register against.</param>
    /// <param name="healthChecks">The health check instances to run.</param>
    public static IHealthCheckBuilder AddHealthChecks(this IHealthCheckBuilder source, params IHealthCheck[] healthChecks)
    {
        foreach (var healthCheck in healthChecks)
        {
            source.AddHealthCheck(_ => healthCheck);
        }

        return source;
    }

    /// <summary>Registers a health check built via an <see cref="IHealthCheckFactory"/>, invoked with the current resolver each time checks run.</summary>
    /// <param name="source">The builder to register against.</param>
    /// <param name="healthCheckFactory">The factory that creates the health check.</param>
    public static IHealthCheckBuilder AddHealthCheckFactory(this IHealthCheckBuilder source, IHealthCheckFactory healthCheckFactory)
    {
        return source.AddHealthCheck(healthCheckFactory.Create);
    }
}
