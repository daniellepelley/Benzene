namespace Benzene.Mesh.Contracts;

/// <summary>
/// Well-known <see cref="MeshUsageEntry.Source"/> values - which adapter produced a usage entry.
/// Adapter packages introduce their own constants alongside these (the field is an open string,
/// like <see cref="TopologyEdge.Source"/>), these exist so the shipped sources agree on spelling.
/// </summary>
public static class MeshUsageSource
{
    /// <summary>
    /// Produced by <c>Benzene.Mesh.Collector</c>'s in-process bridge from its cumulative per-topic
    /// trace stats: counts per (topic, version, status), with no transport or per-service
    /// dimension - the trace wire shape doesn't carry a transport, so a collector-sourced report
    /// deliberately exercises the missing-dimension degradation path instead of guessing.
    /// </summary>
    public const string Collector = "collector";
}
