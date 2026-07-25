namespace Benzene.Mesh.Tracing.Tempo;

/// <summary>
/// The topics this tracing adapter exposes. Carries the <c>benzene:</c> marker per the naming
/// principle (<c>work/benzene-naming-principle.md</c>) — a topic id shares a namespace with the
/// application's own topics, so it says whose it is.
/// </summary>
public static class TempoMeshTopics
{
    /// <summary>Ingests an observed service-to-service topology report.</summary>
    public const string Topology = "benzene:mesh:topology";
}
