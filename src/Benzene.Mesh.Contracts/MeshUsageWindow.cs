namespace Benzene.Mesh.Contracts;

/// <summary>
/// A resolved, absolute <c>[FromUtc, ToUtc]</c> window an <see cref="IMeshUsageSource"/> is asked to scope its
/// counts to. Passed by a caller that has already resolved a relative range (e.g. the mesh UI's picker, via the
/// composite fleet read model) into concrete bounds - this package deliberately stays free of the relative-time
/// grammar, which lives with the read models.
/// </summary>
/// <remarks>
/// A <c>null</c> window (the default on <see cref="IMeshUsageSource.FetchUsageAsync"/>) means "use the source's
/// own configured window" - today's behavior, so the aggregator's <c>usage.json</c> path is unaffected. A source
/// that <em>cannot</em> honor an arbitrary window (a cumulative in-memory counter) ignores it and reports its own
/// window via the returned <see cref="MeshUsage.WindowStartUtc"/>/<see cref="MeshUsage.WindowEndUtc"/>: the caller
/// compares the returned window to the one it asked for to decide whether the counts were actually windowed
/// (rather than a source having to self-certify). A source that <em>can</em> honor it queries its backend over
/// exactly these bounds and echoes them back on the returned report.
/// </remarks>
public sealed class MeshUsageWindow
{
    /// <summary>Initializes a new instance of the <see cref="MeshUsageWindow"/> class.</summary>
    public MeshUsageWindow(DateTimeOffset fromUtc, DateTimeOffset toUtc)
    {
        FromUtc = fromUtc;
        ToUtc = toUtc;
    }

    /// <summary>The absolute lower bound the counts should cover.</summary>
    public DateTimeOffset FromUtc { get; }

    /// <summary>The absolute upper bound the counts should cover.</summary>
    public DateTimeOffset ToUtc { get; }
}
