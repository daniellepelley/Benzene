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
}
