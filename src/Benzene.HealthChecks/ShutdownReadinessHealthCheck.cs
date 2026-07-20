using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>
/// A readiness check that reports <see cref="HealthCheckStatus.Failed"/> once graceful shutdown has
/// begun (see <see cref="ShutdownState"/>), otherwise healthy. Add it to a <b>readiness</b> probe
/// (<c>.UseReadinessCheck(...)</c>) so a load balancer / Kubernetes stops routing new traffic to a
/// draining instance while in-flight work finishes. Do not add it to a liveness probe - a draining
/// instance is healthy, not broken, and failing liveness would trigger a restart mid-drain.
/// </summary>
public class ShutdownReadinessHealthCheck : IHealthCheck
{
    private readonly ShutdownState _shutdownState;

    /// <summary>Initializes a new instance reading <paramref name="shutdownState"/>.</summary>
    /// <param name="shutdownState">The latch tripped when graceful shutdown begins.</param>
    public ShutdownReadinessHealthCheck(ShutdownState shutdownState)
    {
        _shutdownState = shutdownState;
    }

    /// <inheritdoc />
    public string Type => "Shutdown";

    /// <inheritdoc />
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var shuttingDown = _shutdownState.IsShuttingDown;
        var data = new Dictionary<string, object> { { "ShuttingDown", shuttingDown } };
        return Task.FromResult(HealthCheckResult.CreateInstance(!shuttingDown, Type, data));
    }
}
