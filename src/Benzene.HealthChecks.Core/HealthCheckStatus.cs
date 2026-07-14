namespace Benzene.HealthChecks.Core;

/// <summary>The status strings an <see cref="IHealthCheckResult"/> reports.</summary>
public static class HealthCheckStatus
{
    /// <summary>The check passed.</summary>
    public const string Ok = "ok";

    /// <summary>The check found a degraded but non-fatal condition - does not flip an aggregated response's <c>IsHealthy</c> to <c>false</c>.</summary>
    public const string Warning = "warning";

    /// <summary>The check failed - flips an aggregated response's <c>IsHealthy</c> to <c>false</c>.</summary>
    public const string Failed = "failed";
}
