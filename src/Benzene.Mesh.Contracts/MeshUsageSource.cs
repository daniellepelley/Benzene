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

    /// <summary>
    /// Produced by <c>Benzene.Mesh.Usage.CloudWatch</c>, reading the <c>benzene.messages.processed</c>
    /// counter (exported to CloudWatch, e.g. via the ADOT collector's EMF exporter) back as counts per
    /// (topic, transport, status) over a configured window. Carries the <c>transport</c> and outcome
    /// dimensions the collector bridge can't, but no per-service dimension (the metric isn't tagged by
    /// service) - so it exercises the missing-<c>service</c> degradation path.
    /// </summary>
    public const string CloudWatch = "cloudwatch";

    /// <summary>
    /// Produced by <c>Benzene.Mesh.Usage.ApplicationInsights</c>, reading the
    /// <c>benzene.messages.processed</c> counter back from an Azure Monitor / Application Insights
    /// Log Analytics workspace (the <c>customMetrics</c> table) as counts per (topic, transport, status)
    /// over a configured window. The Azure sibling of <see cref="CloudWatch"/>; same dimensions, same
    /// missing-<c>service</c> degradation path.
    /// </summary>
    public const string ApplicationInsights = "application-insights";
}
