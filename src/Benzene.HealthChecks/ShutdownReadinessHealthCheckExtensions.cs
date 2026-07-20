using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>Registration helper for <see cref="ShutdownReadinessHealthCheck"/>.</summary>
public static class ShutdownReadinessHealthCheckExtensions
{
    /// <summary>
    /// Registers a <see cref="ShutdownReadinessHealthCheck"/> reading <paramref name="shutdownState"/>.
    /// Add it to a readiness probe (<c>.UseReadinessCheck(...)</c>); trip the latch by wiring
    /// <paramref name="shutdownState"/> to the host's shutdown token via <see cref="ShutdownState.LinkTo"/>.
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="shutdownState">The shutdown latch to read.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddShutdownReadinessCheck(this IHealthCheckBuilder builder, ShutdownState shutdownState)
    {
        return builder.AddHealthCheck(new ShutdownReadinessHealthCheck(shutdownState));
    }
}
