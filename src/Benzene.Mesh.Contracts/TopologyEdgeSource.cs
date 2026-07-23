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
    /// contracts alone, with no tracing backend. The metric columns (req/min, error rate, latency
    /// percentiles) are left null on a purely-structural edge unless a usage/tracing source can attribute
    /// them; contrast <see cref="Tempo"/>, which carries observed traffic and performance.
    /// </summary>
    public const string Structural = "structural";

    /// <summary>
    /// An "actually calling, and how it's performing" edge derived from observed traffic - real
    /// spans aggregated into service-graph metrics. Produced by
    /// <c>Benzene.Mesh.Tracing.Tempo</c>.
    /// </summary>
    public const string Tempo = "tempo";
}
