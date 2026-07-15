namespace Benzene.Mesh.Contracts;

/// <summary>
/// The origin of a <see cref="TopologyEdge"/> - which technique produced it.
/// </summary>
public static class TopologyEdgeSource
{
    /// <summary>
    /// A "designed to call" edge derived from static/structural information (e.g. which services
    /// generate a <c>Benzene.CodeGen.Client</c> against which other service's spec). Not currently
    /// produced by any Benzene package - deriving this at runtime from an HTTP-polling aggregator
    /// is a real, still-open design question (see <c>work/service-mesh-roadmap-1.0.md</c> §4.6).
    /// Defined here so <see cref="TopologyEdge"/> can represent it once that's built, without a
    /// later breaking change to this constant list.
    /// </summary>
    public const string Structural = "structural";

    /// <summary>
    /// An "actually calling, and how it's performing" edge derived from observed traffic - real
    /// spans aggregated into service-graph metrics. Produced by
    /// <c>Benzene.Mesh.Tracing.Tempo</c>.
    /// </summary>
    public const string Tempo = "tempo";
}
