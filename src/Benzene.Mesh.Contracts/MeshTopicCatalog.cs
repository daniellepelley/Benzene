namespace Benzene.Mesh.Contracts;

/// <summary>
/// The <c>topics.json</c> shape a <c>Benzene.Mesh.Aggregator</c> publishes on each run: every topic
/// seen across the mesh and which service(s) expose it, derived from each service's <c>spec</c>. Gives
/// a catalog UI a platform-wide "who owns which topic" view alongside the per-service catalog.
/// </summary>
public class MeshTopicCatalog
{
    /// <summary>Initializes a new instance of the <see cref="MeshTopicCatalog"/> class.</summary>
    /// <param name="generatedAtUtc">When this catalog was generated.</param>
    /// <param name="topics">One entry per distinct topic across the mesh.</param>
    public MeshTopicCatalog(DateTimeOffset generatedAtUtc, MeshTopicEntry[] topics)
    {
        GeneratedAtUtc = generatedAtUtc;
        Topics = topics;
    }

    /// <summary>When this catalog was generated.</summary>
    public DateTimeOffset GeneratedAtUtc { get; }

    /// <summary>One entry per distinct topic across the mesh.</summary>
    public MeshTopicEntry[] Topics { get; }
}
