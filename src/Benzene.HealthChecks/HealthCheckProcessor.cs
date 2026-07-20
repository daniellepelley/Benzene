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
/// checks share (or omit) a <see cref="IHealthCheck.Type"/>.
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
        var check = new TimeOutHealthCheck(new ExceptionHandlingHealthCheck(healthCheck), _timeout);

        var stopwatch = Stopwatch.StartNew();
        var result = await check.ExecuteAsync();
        stopwatch.Stop();

        return new HealthCheckResult(result.Status, result.Type, result.Data, result.Dependencies, stopwatch.Elapsed);
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
