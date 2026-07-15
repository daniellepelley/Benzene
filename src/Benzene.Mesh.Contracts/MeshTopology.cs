namespace Benzene.Mesh.Contracts;

/// <summary>
/// Cross-service call edges published by a topology source (e.g. <c>Benzene.Mesh.Tracing.Tempo</c>)
/// - the <c>topology.json</c> shape.
/// </summary>
public class MeshTopology
{
    /// <summary>Initializes a new instance of the <see cref="MeshTopology"/> class.</summary>
    /// <param name="generatedAtUtc">When this topology was generated.</param>
    /// <param name="edges">The edges observed or derived in this run.</param>
    public MeshTopology(DateTimeOffset generatedAtUtc, TopologyEdge[] edges)
    {
        GeneratedAtUtc = generatedAtUtc;
        Edges = edges;
    }

    /// <summary>When this topology was generated.</summary>
    public DateTimeOffset GeneratedAtUtc { get; }

    /// <summary>The edges observed or derived in this run.</summary>
    public TopologyEdge[] Edges { get; }
}
