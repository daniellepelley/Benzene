namespace Benzene.HealthChecks.Core;

/// <summary>
/// Adapts a plain <see cref="IHealthCheck"/> into the dependency registration category
/// (<see cref="IDependencyHealthCheck"/>) so the general (<c>healthcheck</c>) probe harvests it while
/// <c>liveness</c>/<c>readiness</c>/<c>contracts</c> ignore it (see <see cref="IDependencyHealthCheck"/>
/// for why a dependency check must stay off the Kubernetes probes). Concrete client checks
/// (<c>SqsHealthCheck</c>, …) stay plain <see cref="IHealthCheck"/> implementations; the auto-wiring
/// registers <c>AddScoped&lt;IDependencyHealthCheck&gt;(r =&gt; new DependencyHealthCheck(build(r), dedupKey))</c>.
/// All the check's own members — including the <see cref="IHealthCheck.Tags"/>/<see cref="IHealthCheck.IsNonCritical"/>/
/// <see cref="IHealthCheck.Ttl"/>/<see cref="IHealthCheck.Timeout"/> overrides — are delegated through
/// unchanged, so wrapping does not alter behaviour.
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

    /// <inheritdoc />
    public bool IsNonCritical => _inner.IsNonCritical;

    /// <inheritdoc />
    public TimeSpan? Ttl => _inner.Ttl;

    /// <inheritdoc />
    public TimeSpan? Timeout => _inner.Timeout;

    /// <inheritdoc />
    public Task<IHealthCheckResult> ExecuteAsync() => _inner.ExecuteAsync();
}
