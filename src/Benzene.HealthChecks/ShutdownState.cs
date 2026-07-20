namespace Benzene.HealthChecks;

/// <summary>
/// A one-way "the process is shutting down" latch, read by <see cref="ShutdownReadinessHealthCheck"/>
/// to fail a readiness probe during graceful shutdown. Wiring it to the host's shutdown signal (via
/// <see cref="LinkTo"/>) is what implements drain-on-SIGTERM: once shutdown starts, the readiness
/// probe returns unhealthy so a load balancer / Kubernetes stops routing new traffic to this instance
/// while in-flight requests finish, before the process actually exits. Liveness is deliberately left
/// healthy (the instance isn't broken, it's draining), so only readiness should use the check.
/// </summary>
public sealed class ShutdownState
{
    private volatile bool _isShuttingDown;

    /// <summary>Whether graceful shutdown has begun. Latches to <c>true</c> and never reverts.</summary>
    public bool IsShuttingDown => _isShuttingDown;

    /// <summary>Marks the process as shutting down. Idempotent; once set it never reverts.</summary>
    public void MarkShuttingDown() => _isShuttingDown = true;

    /// <summary>
    /// Trips <see cref="MarkShuttingDown"/> when <paramref name="shutdownToken"/> is cancelled - pass
    /// the host's shutdown signal (e.g. <c>IHostApplicationLifetime.ApplicationStopping</c> on the
    /// generic host / ASP.NET Core, or a self-hosted worker's stop token). If the token is already
    /// cancelled the latch is set immediately.
    /// </summary>
    /// <param name="shutdownToken">The host shutdown token to observe.</param>
    /// <returns>This instance, for chaining.</returns>
    public ShutdownState LinkTo(CancellationToken shutdownToken)
    {
        if (shutdownToken.IsCancellationRequested)
        {
            MarkShuttingDown();
        }
        else if (shutdownToken.CanBeCanceled)
        {
            shutdownToken.Register(MarkShuttingDown);
        }

        return this;
    }
}
