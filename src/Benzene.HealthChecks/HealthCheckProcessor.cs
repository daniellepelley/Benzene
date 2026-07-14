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
    /// <returns>
    /// An <c>IBenzeneResult</c> whose value is a <c>HealthCheckResponse</c> aggregating every check's
    /// outcome, with status <see cref="BenzeneResultStatus.Ok"/> (HTTP 200 via the standard HTTP
    /// status code mapper) when healthy or <see cref="BenzeneResultStatus.ServiceUnavailable"/> (HTTP
    /// 503) when not - this is what makes the result usable by any health-check consumer that only
    /// inspects the HTTP status code rather than parsing the response body, such as a Kubernetes HTTP
    /// liveness/readiness probe or a load balancer target-health check.
    /// </returns>
    public static async Task<IBenzeneResult> PerformHealthChecksAsync(string topic, IHealthCheck[] healthChecks)
    {
        var runningHealthChecks = healthChecks.Select(x => (x.Type,
            new TimeOutHealthCheck(new ExceptionHandlingHealthCheck(x)).ExecuteAsync())).ToArray();
        var results = await Task.WhenAll(runningHealthChecks.Select(x => x.Item2));
        var isHealthy = results.All(x => x.Status != HealthCheckStatus.Failed);

        var healthCheckNamer = new HealthCheckNamer();
        var message = new HealthCheckResponse(isHealthy, runningHealthChecks.ToDictionary(
            x => healthCheckNamer.GetName(x.Item2.Result.Type),
            x => new HealthCheckResult(x.Item2.Result.Status, x.Item2.Result.Type, x.Item2.Result.Data, x.Item2.Result.Dependencies)));

        return isHealthy
            ? BenzeneResult.Ok(message)
            : BenzeneResult.Set(BenzeneResultStatus.ServiceUnavailable, message);
    }
}
