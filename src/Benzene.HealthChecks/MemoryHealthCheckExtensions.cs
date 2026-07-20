using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>Registration helper for <see cref="MemoryHealthCheck"/>.</summary>
public static class MemoryHealthCheckExtensions
{
    /// <summary>Registers a <see cref="MemoryHealthCheck"/> on this process's working set.</summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="maximumBytes">At or above this many bytes of working set the check fails.</param>
    /// <param name="warningBytes">Optional soft threshold at or above which (but below the maximum) the check warns.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthCheckBuilder AddMemoryCheck(this IHealthCheckBuilder builder, long maximumBytes, long? warningBytes = null)
    {
        return builder.AddHealthCheck(new MemoryHealthCheck(maximumBytes, warningBytes));
    }
}
