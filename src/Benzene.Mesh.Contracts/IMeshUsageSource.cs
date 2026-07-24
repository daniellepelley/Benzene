namespace Benzene.Mesh.Contracts;

/// <summary>
/// Supplies a <see cref="MeshUsage"/> report for the aggregator to publish as <c>usage.json</c> -
/// a port, not an implementation, so a usage adapter (a metrics-backend reader, or the in-process
/// collector bridge) depends on this package alone, same as <see cref="IMeshReportPublisher"/>.
/// </summary>
/// <remarks>
/// The usage signal itself originates from each service's per-message metrics - the
/// <c>benzene.messages.processed</c> counter tagged <c>topic</c>/<c>transport</c>/<c>result</c>
/// (see <c>docs/mesh-usage-feed.md</c>). An implementation of this port reads whichever backend
/// those metrics were exported to (Application Insights, CloudWatch, an OTel collector, or
/// Benzene's own mesh collector) and translates the query result into <see cref="MeshUsageEntry"/>
/// rows, reporting exactly the dimensions the backend can supply. <c>Benzene.Mesh.Aggregator</c>
/// resolves every registered source per run and merges the reports; returning <c>null</c> means
/// "nothing to report this run" (distinct from an empty <see cref="MeshUsage.Entries"/> array,
/// which means "feed is wired, no traffic observed"). A source that throws is skipped for that
/// run without failing it, matching the per-service fetch rule.
/// </remarks>
public interface IMeshUsageSource
{
    /// <summary>Fetches the current usage report, or <c>null</c> when this source has nothing to report.</summary>
    /// <param name="window">
    /// An optional absolute window to scope the counts to (e.g. driven by the mesh UI's time-range picker via the
    /// composite fleet read model). <c>null</c> (the default) means "use this source's own configured window" -
    /// today's behavior, so the aggregator's <c>usage.json</c> path is unaffected. A source that can't honor an
    /// arbitrary window ignores it and reports its own window on the returned <see cref="MeshUsage"/>; the caller
    /// compares the two to tell whether the counts were actually windowed (see <see cref="MeshUsageWindow"/>).
    /// </param>
    /// <param name="cancellationToken">Cancels the fetch (the aggregator bounds each source with its per-fetch timeout).</param>
    Task<MeshUsage?> FetchUsageAsync(MeshUsageWindow? window = null, CancellationToken cancellationToken = default);
}
