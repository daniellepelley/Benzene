namespace Benzene.Mesh.Contracts;

/// <summary>
/// The status values a <see cref="MeshManifestEntry"/> can report. Loose string constants rather
/// than an enum, matching <c>Benzene.HealthChecks.Core.HealthCheckStatus</c>'s existing convention.
/// </summary>
public static class MeshServiceStatus
{
    /// <summary>The service's health endpoint was reachable and reported healthy.</summary>
    public const string Healthy = "healthy";

    /// <summary>The service's health endpoint was reachable but reported unhealthy.</summary>
    public const string Unhealthy = "unhealthy";

    /// <summary>Neither the spec nor the health endpoint could be reached.</summary>
    public const string Unreachable = "unreachable";
}
