using System.Collections.Concurrent;
using Benzene.Abstractions.Results;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// An <see cref="IHealthCheckProcessor"/> decorator that caches the aggregated result for a short TTL,
/// so a busy load balancer or Kubernetes probe polling every few seconds does not re-run every check
/// (and re-hit every external dependency) on every request. Opt-in: register this as the
/// <see cref="IHealthCheckProcessor"/> wrapping a <see cref="HealthCheckProcessor"/> - do NOT use it
/// for a liveness probe that must reflect the instant state.
/// </summary>
/// <remarks>
/// The cache is keyed by the set of check <see cref="IHealthCheck.Type"/>s, so different probes
/// (e.g. liveness vs readiness) that run different check sets cache independently rather than sharing
/// one stale entry. A cold-cache race may run the inner processor a couple of times concurrently
/// (last write wins) - acceptable, since the goal is only to stop *every* probe re-running the checks.
/// </remarks>
public class CachingHealthCheckProcessor : IHealthCheckProcessor
{
    private readonly IHealthCheckProcessor _inner;
    private readonly TimeSpan _ttl;
    private readonly Func<DateTime> _now;
    private readonly ConcurrentDictionary<string, (DateTime CachedAt, IBenzeneResult Result)> _cache = new();

    /// <summary>Initializes a new instance.</summary>
    /// <param name="inner">The processor that actually runs the checks on a cache miss.</param>
    /// <param name="ttl">How long an aggregated result is served from cache before the checks are re-run.</param>
    public CachingHealthCheckProcessor(IHealthCheckProcessor inner, TimeSpan ttl)
        : this(inner, ttl, () => DateTime.UtcNow)
    {
    }

    /// <summary>Initializes a new instance with an injectable clock (for testing).</summary>
    /// <param name="inner">The processor that actually runs the checks on a cache miss.</param>
    /// <param name="ttl">How long an aggregated result is served from cache before the checks are re-run.</param>
    /// <param name="now">The clock used to age cache entries.</param>
    public CachingHealthCheckProcessor(IHealthCheckProcessor inner, TimeSpan ttl, Func<DateTime> now)
    {
        _inner = inner;
        _ttl = ttl;
        _now = now;
    }

    /// <inheritdoc />
    public async Task<IBenzeneResult> PerformHealthChecksAsync(IHealthCheck[] healthChecks)
    {
        var key = string.Join(",", healthChecks.Select(x => x.Type).OrderBy(x => x, StringComparer.Ordinal));

        if (_cache.TryGetValue(key, out var entry) && _now() - entry.CachedAt < _ttl)
        {
            return entry.Result;
        }

        var result = await _inner.PerformHealthChecksAsync(healthChecks);
        _cache[key] = (_now(), result);
        return result;
    }
}
