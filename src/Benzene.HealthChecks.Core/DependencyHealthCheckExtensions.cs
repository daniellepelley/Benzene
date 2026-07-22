using Benzene.Abstractions.DI;

namespace Benzene.HealthChecks.Core;

/// <summary>
/// The config-time registration seam (§3.1 / Phase 1) that auto-wired client extensions call — via
/// <c>app.Register(x =&gt; x.AddDependencyHealthCheck(...))</c> from an <c>IMiddlewarePipelineBuilder</c>
/// extension — to contribute an external-dependency reachability check to the <b>dependency</b> category.
/// Registering under <see cref="IDependencyHealthCheck"/> (rather than plain <see cref="IHealthCheck"/>)
/// is what keeps the check on the deep <c>healthcheck</c> layer and off the liveness/readiness/contracts
/// probes (see <see cref="IDependencyHealthCheck"/> for the cascading-failure reasoning).
/// <para>
/// Lives in <c>HealthChecks.Core</c> — not the middleware package — so a client package can auto-wire
/// using only its existing lightweight <c>HealthChecks.Core</c> reference, without pulling in the full
/// health-check middleware.
/// </para>
/// </summary>
public static class DependencyHealthCheckExtensions
{
    /// <summary>
    /// Registers a dependency-category health check built from <paramref name="factory"/>. Harvested by the
    /// general (<c>healthcheck</c>) probe only — never <c>liveness</c>/<c>readiness</c>/<c>contracts</c>.
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
    public static IBenzeneServiceContainer AddDependencyHealthCheck(
        this IBenzeneServiceContainer services, Func<IServiceResolver, IHealthCheck> factory, string? dedupKey = null)
    {
        return services.AddScoped<IDependencyHealthCheck>(resolver => new DependencyHealthCheck(factory(resolver), dedupKey));
    }
}
