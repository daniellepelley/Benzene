namespace Benzene.Mesh.Contracts;

/// <summary>
/// The origin of a <see cref="TopologyEdge"/> - which technique produced it.
/// </summary>
public static class TopologyEdgeSource
{
    /// <summary>
    /// A "designed to call" edge derived from the fleet's declared contracts: for every domain topic a
    /// service <em>produces</em> (spec <c>events</c>), an edge to every service that <em>consumes</em> it
    /// (spec <c>requests</c>). Produced by <c>Benzene.Mesh.Aggregator.MeshAggregator.BuildTopology</c> and
    /// published as <c>topology.json</c> on every run - so a mesh has a topology graph from declared
    /// contracts alone, with no tracing backend. A structural edge's <c>RequestsPerMinute</c>/
    /// <c>ErrorRate</c> are populated from the usage feed (<c>usage.json</c>) when the aggregator can
    /// attribute a topic's observed traffic to that specific edge unambiguously (single-producer rule -
    /// see <c>Benzene.Mesh.Aggregator</c>'s <c>AttributeTopicToEdge</c>), and left null otherwise;
    /// latency percentiles are never available from that feed and stay null. Contrast <see cref="Tempo"/>,
    /// which carries observed traffic and performance (including latency) from real spans.
    /// </summary>
    public const string Structural = "structural";

    /// <summary>
    /// An "actually calling, and how it's performing" edge derived from observed traffic - real
    /// spans aggregated into service-graph metrics. Produced by
    /// <c>Benzene.Mesh.Tracing.Tempo</c>.
    /// </summary>
    public const string Tempo = "tempo";
}
