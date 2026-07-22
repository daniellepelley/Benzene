using System.Diagnostics;
using Benzene.Abstractions.Results;
using Benzene.HealthChecks.Core;
using Benzene.Results;

namespace Benzene.HealthChecks;

/// <summary>
/// Default <see cref="IHealthCheckProcessor"/>: runs every check concurrently, each wrapped in a
/// <see cref="TimeOutHealthCheck"/> (configurable timeout) around an
/// <see cref="ExceptionHandlingHealthCheck"/>, times each one, and aggregates into a
/// <see cref="HealthCheckResponse"/>. <c>IsHealthy</c> is <c>true</c> unless at least one check
/// reports <see cref="HealthCheckStatus.Failed"/> - a <see cref="HealthCheckStatus.Warning"/> does not
/// flip it. Each result's key is assigned via <see cref="HealthCheckNamer"/> to stay unique even when
/// checks share (or omit) a <see cref="IHealthCheck.Type"/>. Per-check overrides are honoured: a check's
/// <see cref="IHealthCheck.Timeout"/> replaces the processor-wide timeout, and a non-critical check
/// (<see cref="IHealthCheck.IsNonCritical"/> == <c>true</c>) has a <see cref="HealthCheckStatus.Failed"/>
/// result downgraded to <see cref="HealthCheckStatus.Warning"/> so it never flips the probe unhealthy -
/// <em>unless</em> that failure is persistent (<see cref="IHealthCheckResult.IsPersistent"/>, e.g. an
/// authorization denial), which escapes the downgrade and stays <see cref="HealthCheckStatus.Failed"/>.
/// </summary>
public class HealthCheckProcessor : IHealthCheckProcessor
{
    /// <summary>The timeout applied to every check when none is configured.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly TimeSpan _timeout;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The per-check timeout. Defaults to <see cref="DefaultTimeout"/> (10s) when null.</param>
    public HealthCheckProcessor(TimeSpan? timeout = null)
    {
        _timeout = timeout ?? DefaultTimeout;
    }

    /// <inheritdoc />
    public async Task<IBenzeneResult> PerformHealthChecksAsync(IHealthCheck[] healthChecks)
    {
        var results = await Task.WhenAll(healthChecks.Select(RunTimedAsync));
        var isHealthy = results.All(x => x.Status != HealthCheckStatus.Failed);

        var healthCheckNamer = new HealthCheckNamer();
        var message = new HealthCheckResponse(isHealthy,
            results.ToDictionary(x => healthCheckNamer.GetName(x.Type), x => x));

        // Unhealthy: ServiceUnavailable so HTTP probes see a 503, but explicitly successful so
        // the response body renders the health report payload rather than an error payload.
        return isHealthy
            ? BenzeneResult.Ok(message)
            : BenzeneResult.Set(BenzeneResultStatus.ServiceUnavailable, message, true);
    }

    // Times a single check and stamps the measured duration onto its (rebuilt) result. Runs
    // concurrently with the other checks - each has its own stopwatch, so there is no shared state.
    private async Task<HealthCheckResult> RunTimedAsync(IHealthCheck healthCheck)
    {
        // A check may override the processor-wide timeout (IHealthCheck.Timeout) - honour it here.
        var check = new TimeOutHealthCheck(new ExceptionHandlingHealthCheck(healthCheck), healthCheck.Timeout ?? _timeout);

        var stopwatch = Stopwatch.StartNew();
        var result = await check.ExecuteAsync();
        stopwatch.Stop();

        // A non-critical check (IHealthCheck.IsNonCritical == true) normally never flips the probe
        // unhealthy: a Failed result is downgraded to Warning so a non-critical dependency being down
        // degrades the instance rather than taking it out of service (§3.4). The exception is a
        // *persistent* failure (IHealthCheckResult.IsPersistent) - a deterministic fault like an
        // authorization denial that won't self-heal - which escapes the downgrade and stays Failed, so a
        // real misconfiguration surfaces as unhealthy on the deep layer rather than sitting yellow forever.
        var status = result.Status == HealthCheckStatus.Failed && healthCheck.IsNonCritical && !result.IsPersistent
            ? HealthCheckStatus.Warning
            : result.Status;

        return new HealthCheckResult(status, result.Type, result.Data, result.Dependencies, stopwatch.Elapsed, result.IsPersistent);
    }

    /// <summary>
    /// Convenience entry point that runs the checks with the default timeout. The <paramref name="topic"/>
    /// is accepted for source-compatibility but not used; prefer resolving <see cref="IHealthCheckProcessor"/>.
    /// </summary>
    public static Task<IBenzeneResult> PerformHealthChecksAsync(string topic, IHealthCheck[] healthChecks)
    {
        return new HealthCheckProcessor().PerformHealthChecksAsync(healthChecks);
    }
}
