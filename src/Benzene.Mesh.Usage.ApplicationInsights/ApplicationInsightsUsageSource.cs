using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Usage.ApplicationInsights;

/// <summary>
/// An <see cref="IMeshUsageSource"/> that reads the <c>benzene.messages.processed</c> counter back from
/// Azure Monitor / Application Insights and reports it as the mesh usage feed: a count per
/// (topic, transport, status) over a configured window. The counter is emitted by every service's
/// <c>UseBenzeneMetrics()</c> middleware (tags <c>topic</c>/<c>transport</c>/<c>result</c>) and exported to
/// Application Insights by the Azure Monitor OpenTelemetry exporter, where it lands in the Log Analytics
/// <c>customMetrics</c> table; this source is the aggregator-side reader that turns those metrics into
/// <c>usage.json</c>. The Azure sibling of <c>Benzene.Mesh.Usage.CloudWatch</c>.
/// </summary>
/// <remarks>
/// It reports the dimensions the metric carries (topic, transport, and the outcome as <c>status</c>) and
/// leaves the ones it can't (<c>version</c>, <c>service</c>, <c>avgDurationMs</c>) <c>null</c>, per the
/// missing-dimension rule. It assumes the counter is exported with <b>delta</b> temporality so a sum of
/// <c>valueSum</c> over the window equals the request count; a cumulative export would over-count. An empty
/// result is reported as an empty <see cref="MeshUsage.Entries"/> - "feed wired, no traffic observed" -
/// never <c>null</c>; a genuine query failure propagates (the aggregator bounds and skips a throwing source).
/// </remarks>
public class ApplicationInsightsUsageSource : IMeshUsageSource
{
    private readonly IApplicationInsightsUsageQuery _query;
    private readonly ApplicationInsightsUsageOptions _options;

    /// <summary>Initializes a new instance of the <see cref="ApplicationInsightsUsageSource"/> class.</summary>
    /// <param name="query">The query seam that runs the grouped count query against the backend.</param>
    /// <param name="options">Which workspace/metric to read, and over what window.</param>
    public ApplicationInsightsUsageSource(IApplicationInsightsUsageQuery query, ApplicationInsightsUsageOptions options)
    {
        _query = query;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<MeshUsage?> FetchUsageAsync(CancellationToken cancellationToken = default)
    {
        var end = DateTimeOffset.UtcNow;
        var start = end - _options.TimeWindow;

        var rows = await _query.QueryAsync(_options, _options.TimeWindow, cancellationToken);

        var entries = new List<MeshUsageEntry>(rows.Count);
        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.Topic) || row.Count <= 0)
            {
                // Not one of ours (no topic) or not exercised in this window - never guess a row.
                continue;
            }

            entries.Add(new MeshUsageEntry(
                topic: row.Topic,
                version: null,
                service: null,
                transport: row.Transport,
                status: row.Result,
                count: row.Count,
                avgDurationMs: null,
                source: MeshUsageSource.ApplicationInsights));
        }

        return new MeshUsage(end, start, end, entries.ToArray());
    }
}
