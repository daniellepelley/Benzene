namespace Benzene.Mesh.Contracts;

/// <summary>
/// One usage count in a <see cref="MeshUsage"/> report: how many messages were handled for
/// <see cref="Topic"/>, at exactly the dimensions this entry states.
/// </summary>
/// <remarks>
/// Granularity rule: an entry is a count at the finest dimension combination its source could
/// supply, entries from one source never overlap (no double counting within a source), and a
/// consumer aggregates by grouping over whichever stated dimensions it needs. A <c>null</c>
/// dimension means the source's backend genuinely doesn't have it (e.g. a metrics backend
/// without the <c>transport</c> tag), not "all" - consumers should surface the gap, not guess.
/// Each entry carries its own <see cref="Source"/>, following the <see cref="TopologyEdge"/>
/// precedent, so one <c>usage.json</c> can merge multiple adapters without losing attribution.
/// </remarks>
public class MeshUsageEntry
{
    /// <summary>Initializes a new instance of the <see cref="MeshUsageEntry"/> class.</summary>
    /// <param name="topic">The topic id the count is for.</param>
    /// <param name="version">The topic version, or <c>null</c> when the source doesn't discriminate versions.</param>
    /// <param name="service">The handling service, or <c>null</c> when the source can't attribute counts per service.</param>
    /// <param name="transport">The transport the messages arrived over, or <c>null</c> when the source doesn't have that dimension.</param>
    /// <param name="status">The result status (wire vocabulary, e.g. <c>Ok</c>) or the metric standard's <c>success</c>/<c>failure</c> class, or <c>null</c> when not split by outcome.</param>
    /// <param name="count">How many messages were handled at these dimensions within the report's window.</param>
    /// <param name="avgDurationMs">Mean handling duration in milliseconds at these dimensions, or <c>null</c> if the source doesn't measure it.</param>
    /// <param name="source">Which adapter produced this entry (e.g. <c>"collector"</c>) - see <see cref="MeshUsageSource"/>.</param>
    public MeshUsageEntry(
        string topic,
        string? version,
        string? service,
        string? transport,
        string? status,
        long count,
        double? avgDurationMs,
        string source)
    {
        Topic = topic;
        Version = version;
        Service = service;
        Transport = transport;
        Status = status;
        Count = count;
        AvgDurationMs = avgDurationMs;
        Source = source;
    }

    /// <summary>The topic id the count is for.</summary>
    public string Topic { get; }

    /// <summary>The topic version, or <c>null</c> when the source doesn't discriminate versions.</summary>
    public string? Version { get; }

    /// <summary>The handling service, or <c>null</c> when the source can't attribute counts per service.</summary>
    public string? Service { get; }

    /// <summary>The transport the messages arrived over, or <c>null</c> when the source doesn't have that dimension.</summary>
    public string? Transport { get; }

    /// <summary>The result status, or <c>null</c> when not split by outcome.</summary>
    public string? Status { get; }

    /// <summary>How many messages were handled at these dimensions within the report's window.</summary>
    public long Count { get; }

    /// <summary>Mean handling duration in milliseconds at these dimensions, or <c>null</c> if the source doesn't measure it.</summary>
    public double? AvgDurationMs { get; }

    /// <summary>Which adapter produced this entry - see <see cref="MeshUsageSource"/>.</summary>
    public string Source { get; }
}
