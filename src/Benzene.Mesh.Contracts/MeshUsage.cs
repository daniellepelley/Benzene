namespace Benzene.Mesh.Contracts;

/// <summary>
/// Observed topic-usage counts published by one or more usage sources - the <c>usage.json</c>
/// shape. Answers the product question "how often is each topic actually exercised, and over
/// which transports" from observed traffic, complementing the purely structural signals in
/// <c>topics.json</c> (which can say a topic is <em>wired</em>, never that it's <em>used</em>).
/// </summary>
/// <remarks>
/// This is an aggregator-computed artifact, not a Benzene wire contract, and deliberately not
/// part of the Cloud Service spec: usage is an observability concern, fed by each service's
/// per-message metrics (see <c>docs/mesh-usage-feed.md</c> - the metric metadata standard), not
/// by any new endpoint on the service itself. Every dimension beyond the topic is nullable:
/// an adapter reports exactly the dimensions its backend can supply, and a consumer (the Mesh
/// UI) renders what's present and surfaces what's missing rather than failing - the same
/// degradation rule the rest of the mesh follows.
/// </remarks>
public class MeshUsage
{
    /// <summary>Initializes a new instance of the <see cref="MeshUsage"/> class.</summary>
    /// <param name="generatedAtUtc">When this usage report was generated.</param>
    /// <param name="windowStartUtc">Start of the observation window the counts cover, or <c>null</c> when the source can't bound it.</param>
    /// <param name="windowEndUtc">End of the observation window the counts cover, or <c>null</c> when the source can't bound it.</param>
    /// <param name="entries">The usage counts observed in this window.</param>
    public MeshUsage(
        DateTimeOffset generatedAtUtc,
        DateTimeOffset? windowStartUtc,
        DateTimeOffset? windowEndUtc,
        MeshUsageEntry[] entries)
    {
        GeneratedAtUtc = generatedAtUtc;
        WindowStartUtc = windowStartUtc;
        WindowEndUtc = windowEndUtc;
        Entries = entries;
    }

    /// <summary>When this usage report was generated.</summary>
    public DateTimeOffset GeneratedAtUtc { get; }

    /// <summary>Start of the observation window the counts cover, or <c>null</c> when the source can't bound it.</summary>
    public DateTimeOffset? WindowStartUtc { get; }

    /// <summary>End of the observation window the counts cover, or <c>null</c> when the source can't bound it.</summary>
    public DateTimeOffset? WindowEndUtc { get; }

    /// <summary>The usage counts observed in this window.</summary>
    public MeshUsageEntry[] Entries { get; }
}
