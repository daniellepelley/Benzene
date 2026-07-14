namespace Benzene.HealthChecks;

/// <summary>Fixed values used by the health check middleware.</summary>
public static class Constants
{
    /// <summary>The name assigned to the middleware registered by <see cref="Extensions"/>'s <c>UseHealthCheck</c> overloads, used to identify it in the pipeline.</summary>
    public const string HealthCheckMiddlewareName = "Health Check";

    /// <summary>
    /// A message topic that the health check middleware always responds to, in addition to whatever
    /// topic it was configured with. See <see cref="Extensions"/>'s <c>UseHealthCheck</c> overloads.
    /// </summary>
    public const string DefaultHealthCheckTopic = "healthcheck";

    /// <summary>
    /// The message topic used by <see cref="Extensions"/>'s <c>UseLivenessCheck</c> overloads. Unlike
    /// <see cref="DefaultHealthCheckTopic"/>, this is the ONLY topic a liveness check middleware
    /// responds to - it does not also match <see cref="DefaultHealthCheckTopic"/>, so that
    /// <c>UseLivenessCheck</c> and <c>UseReadinessCheck</c> can be registered in the same pipeline
    /// without one silently shadowing the other on a shared fallback topic.
    /// </summary>
    public const string DefaultLivenessTopic = "liveness";

    /// <summary>
    /// The message topic used by <see cref="Extensions"/>'s <c>UseReadinessCheck</c> overloads. See
    /// <see cref="DefaultLivenessTopic"/> for why this doesn't also match
    /// <see cref="DefaultHealthCheckTopic"/>.
    /// </summary>
    public const string DefaultReadinessTopic = "readiness";
}
