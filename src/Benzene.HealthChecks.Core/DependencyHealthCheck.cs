namespace Benzene.HealthChecks.Core;

/// <summary>
/// Adapts a plain <see cref="IHealthCheck"/> into the dependency registration category
/// (<see cref="IDependencyHealthCheck"/>) so the general (<c>healthcheck</c>) probe harvests it while
/// <c>liveness</c>/<c>readiness</c>/<c>contracts</c> ignore it (see <see cref="IDependencyHealthCheck"/>
/// for why a dependency check must stay off the Kubernetes probes). Concrete client checks
/// (<c>SqsHealthCheck</c>, …) stay plain <see cref="IHealthCheck"/> implementations; the auto-wiring
/// registers <c>AddScoped&lt;IDependencyHealthCheck&gt;(r =&gt; new DependencyHealthCheck(build(r), dedupKey))</c>.
/// The check's <see cref="IHealthCheck.Tags"/>/<see cref="IHealthCheck.Ttl"/>/<see cref="IHealthCheck.Timeout"/>
/// overrides are delegated through unchanged.
/// <para>
/// <b>The category is non-critical by default</b> (<see cref="IsNonCritical"/> is forced <c>true</c>): an
/// unreachable dependency <em>degrades</em> the deep <c>healthcheck</c> report to a
/// <see cref="HealthCheckStatus.Warning"/> rather than flipping the aggregate unhealthy. That layer is a
/// monitoring surface, not a probe (it takes no automated Kubernetes action), so a downstream blip must
/// not turn the endpoint into a 503 — the per-dependency Warning is still visible to monitoring / the mesh.
/// A caller who wants a dependency to be fatal registers an explicit critical check instead of relying on
/// the auto-wired default.
/// </para>
/// </summary>
public class DependencyHealthCheck : IDependencyHealthCheck
{
    private readonly IHealthCheck _inner;

    /// <summary>Initializes a new instance wrapping <paramref name="inner"/>.</summary>
    /// <param name="inner">The dependency check to run under the dependency category.</param>
    /// <param name="dedupKey">
    /// A stable de-duplication key (Phase 1 sets it to the dependency's <c>(Type, Name)</c>). When null,
    /// falls back to the inner check's <see cref="IHealthCheck.Type"/>.
    /// </param>
    public DependencyHealthCheck(IHealthCheck inner, string? dedupKey = null)
    {
        _inner = inner;
        DedupKey = dedupKey ?? inner.Type;
    }

    /// <inheritdoc />
    public string Type => _inner.Type;

    /// <inheritdoc />
    public string DedupKey { get; }

    /// <inheritdoc />
    public string[] Tags => _inner.Tags;

    /// <summary>
    /// Always <c>true</c>: a dependency-category check is non-critical, so a failure degrades the deep
    /// <c>healthcheck</c> report to a Warning rather than flipping the aggregate unhealthy (see the type
    /// remarks). This overrides the inner check's value — the category, not the individual check, decides.
    /// </summary>
    public bool IsNonCritical => true;

    /// <inheritdoc />
    public TimeSpan? Ttl => _inner.Ttl;

    /// <inheritdoc />
    public TimeSpan? Timeout => _inner.Timeout;

    /// <inheritdoc />
    public Task<IHealthCheckResult> ExecuteAsync() => _inner.ExecuteAsync();
}
