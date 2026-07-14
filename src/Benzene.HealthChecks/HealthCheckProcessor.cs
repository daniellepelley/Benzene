using Benzene.Abstractions.Results;
using Benzene.HealthChecks.Core;
using Benzene.Results;

namespace Benzene.HealthChecks;

/// <summary>Runs a set of health checks and aggregates their outcomes into a single result.</summary>
public static class HealthCheckProcessor
{
    /// <summary>
    /// Runs every check in <paramref name="healthChecks"/> concurrently and aggregates the results.
    /// Each check is wrapped, before running, in a <c>TimeOutHealthCheck</c> (so it is reported as failed
    /// with an <c>"Error"</c>/<c>"Timed Out"</c> data entry if it does not complete within the timeout)
    /// around an <c>ExceptionHandlingHealthCheck</c> (so an exception thrown by the check is reported as a
    /// failed result rather than propagating and aborting the whole run) - callers/implementations of
    /// <see cref="IHealthCheck"/> do not need to implement their own timeout or exception handling. The
    /// aggregated result's <c>IsHealthy</c> is <c>true</c> unless at least one check reports
    /// <see cref="HealthCheckStatus.Failed"/>; a <see cref="HealthCheckStatus.Warning"/> result does not
    /// flip it to <c>false</c>. Each check's key in the resulting <c>HealthChecks</c> dictionary is
    /// assigned via <see cref="HealthCheckNamer"/> to keep it unique even when multiple checks share the
    /// same (or an empty) <see cref="IHealthCheck.Type"/>.
    /// </summary>
    /// <param name="topic">The topic the health checks were run for. Currently unused by this method itself, but accepted for callers that need to correlate the run with a topic.</param>
    /// <param name="healthChecks">The checks to run.</param>
    /// <returns>A successful <c>IBenzeneResult</c> whose value is a <c>HealthCheckResponse</c> aggregating every check's outcome.</returns>
    public static async Task<IBenzeneResult> PerformHealthChecksAsync(string topic, IHealthCheck[] healthChecks)
    {
        var runningHealthChecks = healthChecks.Select(x => (x.Type, 
            new TimeOutHealthCheck(new ExceptionHandlingHealthCheck(x)).ExecuteAsync())).ToArray();
        var results = await Task.WhenAll(runningHealthChecks.Select(x => x.Item2));
        var isHealthy = results.All(x => x.Status != HealthCheckStatus.Failed);

        var healthCheckNamer = new HealthCheckNamer();
        var message = new HealthCheckResponse(isHealthy, runningHealthChecks.ToDictionary(
            x => healthCheckNamer.GetName(x.Item2.Result.Type),
            x => new HealthCheckResult(x.Item2.Result.Status, x.Item2.Result.Type, x.Item2.Result.Data)));

        return BenzeneResult.Ok(message);
    }
}
