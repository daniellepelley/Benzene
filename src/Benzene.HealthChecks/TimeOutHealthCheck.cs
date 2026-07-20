using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// Decorates an <see cref="IHealthCheck"/> with a fixed 10-second timeout: if the wrapped check has not
/// completed within that time, <see cref="ExecuteAsync"/> returns a failed result (with an
/// <c>"Error"</c>/<c>"Timed Out"</c> data entry) instead of continuing to wait. The timeout is not
/// currently configurable. Used internally by <see cref="HealthCheckProcessor"/> to wrap every check.
/// </summary>
/// <remarks>
/// This only stops <em>waiting</em> on the inner check - the inner <see cref="ExecuteAsync"/> task is not
/// cancelled and keeps running to completion in the background even after a timeout is reported.
/// </remarks>
internal class TimeOutHealthCheck : IHealthCheck
{
    private readonly IHealthCheck _inner;

    /// <inheritdoc />
    public string Type => _inner.Type;

    /// <summary>Initializes a new instance of the <see cref="TimeOutHealthCheck"/> class.</summary>
    /// <param name="inner">The health check to run under a timeout.</param>
    public TimeOutHealthCheck(IHealthCheck inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Runs the wrapped check, waiting up to 10 seconds. If it has not completed by then, returns a
    /// failed result instead of the check's actual outcome.
    /// </summary>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var task = _inner.ExecuteAsync();

        // Cancel the delay as soon as the check wins, so a fast check doesn't leave a 10s timer
        // registered until it fires (health probes run continuously - those timers add up).
        using var cts = new CancellationTokenSource();
        var completed = await Task.WhenAny(task, Task.Delay(10000, cts.Token));
        if (completed == task)
        {
            cts.Cancel();
            // await (not .Result): unwraps to the real exception rather than an AggregateException.
            // The check may still fault if TimeOutHealthCheck is used without the exception-handling
            // decorator, so guard defensively rather than relying on composition order.
            try
            {
                return await task;
            }
            catch (Exception ex)
            {
                return HealthCheckResult.CreateInstance(false, _inner.Type, new Dictionary<string, object>
                {
                    { "Exception", ex.GetType().Name }
                });
            }
        }

        return HealthCheckResult.CreateInstance(false, _inner.Type, new Dictionary<string, object>
            {
                { "Error", "Timed Out"}
            });
    }
}
