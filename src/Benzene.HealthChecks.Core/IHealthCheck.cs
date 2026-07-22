namespace Benzene.HealthChecks.Core;

/// <summary>
/// A single health check: verifies one dependency or condition (e.g. database connectivity, a
/// downstream HTTP service) and reports the outcome. Implementations are typically registered via
/// <see cref="IHealthCheckBuilder"/> and run together by whatever health-check middleware/endpoint
/// aggregates their results into an <see cref="IHealthCheckResponse{THealthCheckResult}"/>.
/// </summary>
public interface IHealthCheck
{
    /// <summary>A short identifier for this check, used as its key in the aggregated response (e.g. "Database", "HttpPing").</summary>
    string Type { get; }

    /// <summary>
    /// Runs the check and returns its outcome. Should not throw for expected failure conditions
    /// (e.g. connection refused) - report them via a failed <see cref="IHealthCheckResult"/> instead.
    /// </summary>
    Task<IHealthCheckResult> ExecuteAsync();

    // The four members below are default interface members: adding them is source- and binary-compatible
    // for the 20+ existing IHealthCheck implementers, which keep the defaults. They exist so a check can
    // describe its own routing/criticality/cost rather than have those decided processor-wide.

    /// <summary>
    /// Open-string labels for routing/filtering a check (e.g. selecting a subset for a probe, or mapping
    /// to a <c>Microsoft.Extensions.Diagnostics.HealthChecks</c> predicate). Defaults to none. Note:
    /// liveness/readiness <em>probe separation</em> is done by dedicated topic + the readiness
    /// registration category, not by a tag - tags are for finer filtering on top of that.
    /// </summary>
    string[] Tags => Array.Empty<string>();

    /// <summary>
    /// Whether a failure of this check should <b>not</b> make the whole probe unhealthy. Defaults to
    /// <c>false</c> - a check is critical unless it opts out, so a <see cref="HealthCheckStatus.Failed"/>
    /// flips the aggregated response to unhealthy. Set to <c>true</c> to have a failure downgraded to
    /// <see cref="HealthCheckStatus.Warning"/> during aggregation, so a non-critical dependency being down
    /// degrades but does not take the instance out of service (§3.4).
    /// <para>
    /// The polarity is deliberate: this is a health-gating decision, so the default/unknown value must
    /// <b>fail safe</b> (critical). A <c>bool IsCritical =&gt; true</c> would read better but is fail-open -
    /// any mock or DI proxy over <see cref="IHealthCheck"/> returns <c>default(bool)</c> == <c>false</c>
    /// for an un-set-up member, which would silently turn a failing dependency non-fatal. With this
    /// polarity, <c>default(bool)</c> == <c>false</c> == critical, the safe outcome.
    /// </para>
    /// </summary>
    bool IsNonCritical => false;

    /// <summary>
    /// A per-check cache lifetime hint for a caching processor, overriding its processor-wide TTL. Defaults
    /// to <c>null</c> (use the processor's TTL). Reserved for a future per-check caching layer - the current
    /// <c>CachingHealthCheckProcessor</c> caches the aggregate per probe, not per check.
    /// </summary>
    TimeSpan? Ttl => null;

    /// <summary>
    /// A per-check timeout, overriding the processor-wide timeout. Defaults to <c>null</c> (use the
    /// processor's timeout). Lets a known-slow check have a longer budget, or a must-be-fast one a shorter
    /// budget, without widening the timeout for every check.
    /// </summary>
    TimeSpan? Timeout => null;
}

