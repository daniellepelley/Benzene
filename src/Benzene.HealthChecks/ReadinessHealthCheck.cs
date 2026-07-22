using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// Adapts a plain <see cref="IHealthCheck"/> into the readiness registration category
/// (<see cref="IReadinessHealthCheck"/>) so the general (<c>healthcheck</c>) and <c>readiness</c> probes
/// harvest it while <c>liveness</c>/<c>contracts</c> ignore it (§3.2). Concrete client checks
/// (<c>SqsHealthCheck</c>, …) stay plain <see cref="IHealthCheck"/> implementations; the auto-wiring
/// registers <c>AddScoped&lt;IReadinessHealthCheck&gt;(r =&gt; new ReadinessHealthCheck(build(r), dedupKey))</c>.
/// All the check's own members — including the <see cref="IHealthCheck.Tags"/>/<see cref="IHealthCheck.IsNonCritical"/>/
/// <see cref="IHealthCheck.Ttl"/>/<see cref="IHealthCheck.Timeout"/> overrides — are delegated through
/// unchanged, so wrapping does not alter behaviour.
/// </summary>
public class ReadinessHealthCheck : IReadinessHealthCheck
{
    private readonly IHealthCheck _inner;

    /// <summary>Initializes a new instance wrapping <paramref name="inner"/>.</summary>
    /// <param name="inner">The dependency check to run under the readiness category.</param>
    /// <param name="dedupKey">
    /// A stable de-duplication key (Phase 1 sets it to the dependency's <c>(Type, Name)</c>). When null,
    /// falls back to the inner check's <see cref="IHealthCheck.Type"/>.
    /// </param>
    public ReadinessHealthCheck(IHealthCheck inner, string? dedupKey = null)
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
