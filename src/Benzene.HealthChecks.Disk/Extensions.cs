using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Disk;

/// <summary>Registration helper for <see cref="DiskHealthCheck"/>.</summary>
public static class Extensions
{
    /// <summary>Registers a <see cref="DiskHealthCheck"/> for the drive containing <paramref name="path"/>.</summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="path">A path on the drive to check.</param>
    /// <param name="minimumFreeBytes">Below this many free bytes the check fails.</param>
    /// <param name="warningFreeBytes">Optional soft threshold below which the check warns.</param>
    public static IHealthCheckBuilder AddDiskSpaceCheck(this IHealthCheckBuilder builder, string path, long minimumFreeBytes, long? warningFreeBytes = null)
    {
        return builder.AddHealthCheck(new DiskHealthCheck(path, minimumFreeBytes, warningFreeBytes));
    }
}
