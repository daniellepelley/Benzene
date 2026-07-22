using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// The config-time registration seam (§3.1 / Phase 0c) that auto-wired client extensions call — via
/// <c>app.Register(x =&gt; x.AddReadinessHealthCheck(...))</c> from an <c>IMiddlewarePipelineBuilder</c>
/// extension — to contribute an external-dependency reachability check to the <b>readiness</b> category.
/// Registering under <see cref="IReadinessHealthCheck"/> (rather than plain <see cref="IHealthCheck"/>)
/// is what keeps the check off the liveness/contracts probes (§3.2).
/// </summary>
public static class ReadinessHealthCheckExtensions
{
    /// <summary>
    /// Registers a readiness-category health check built from <paramref name="factory"/>. Harvested by the
    /// general (<c>healthcheck</c>) and <c>readiness</c> probes, never by <c>liveness</c>/<c>contracts</c>.
    /// De-duplicated by <paramref name="dedupKey"/> so registering the same dependency twice yields one
    /// check — Phase 1 auto-wiring passes the dependency's <c>(Type, Name)</c>.
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="factory">
    /// Builds the underlying check per request against the current <see cref="IServiceResolver"/> — reuse
    /// the client's own SDK handle here (resolve it, or close over an instance the client was handed).
    /// </param>
    /// <param name="dedupKey">A stable de-dup key; when null, falls back to the built check's <see cref="IHealthCheck.Type"/>.</param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddReadinessHealthCheck(
        this IBenzeneServiceContainer services, Func<IServiceResolver, IHealthCheck> factory, string? dedupKey = null)
    {
        return services.AddScoped<IReadinessHealthCheck>(resolver => new ReadinessHealthCheck(factory(resolver), dedupKey));
    }
}
