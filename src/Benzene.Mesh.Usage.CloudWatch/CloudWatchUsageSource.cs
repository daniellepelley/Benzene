using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Usage.CloudWatch;

/// <summary>
/// An <see cref="IMeshUsageSource"/> that reads the <c>benzene.messages.processed</c> counter back from
/// CloudWatch and reports it as the mesh usage feed: a count per (topic, transport, status) over a
/// configured window. The counter is emitted by every service's <c>UseBenzeneMetrics()</c> middleware
/// (tags <c>topic</c>/<c>transport</c>/<c>result</c>) and exported to CloudWatch by the ADOT collector's
/// EMF exporter; this source is the aggregator-side reader that turns those metrics into <c>usage.json</c>.
/// </summary>
/// <remarks>
/// It reports the dimensions the metric carries (topic, transport, and the outcome as <c>status</c>) and
/// leaves the ones it can't (<c>version</c>, <c>service</c>, <c>avgDurationMs</c>) <c>null</c>, per the
/// missing-dimension rule - the Mesh UI renders what's present and flags what's absent. It assumes the
/// counter is exported with <b>delta</b> temporality (the EMF default), so a CloudWatch <c>Sum</c> over the
/// window equals the request count; a cumulative export would over-count. It first lists the metric's live
/// dimension combinations (<c>ListMetrics</c>) and then sums each with one <c>GetMetricData</c> query, so
/// every reported entry's dimensions are known exactly rather than parsed from a grouped label. Only genuine
/// connection-level failures surface (the aggregator bounds and skips a throwing source); an empty result is
/// reported as an empty <see cref="MeshUsage.Entries"/> - "feed wired, no traffic observed".
/// </remarks>
public class CloudWatchUsageSource : IMeshUsageSource
{
    // CloudWatch caps GetMetricData at 500 queries per call.
    private const int MaxQueriesPerCall = 500;

    private readonly IAmazonCloudWatch _cloudWatch;
    private readonly CloudWatchUsageOptions _options;

    /// <summary>Initializes a new instance of the <see cref="CloudWatchUsageSource"/> class.</summary>
    /// <param name="cloudWatch">The CloudWatch client used to query the metric.</param>
    /// <param name="options">Which metric to read, and over what window.</param>
    public CloudWatchUsageSource(IAmazonCloudWatch cloudWatch, CloudWatchUsageOptions options)
    {
        _cloudWatch = cloudWatch;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<MeshUsage?> FetchUsageAsync(CancellationToken cancellationToken = default)
    {
        var end = DateTimeOffset.UtcNow;
        var start = end - _options.TimeWindow;

        var metrics = await ListMetricsAsync(cancellationToken);
        if (metrics.Count == 0)
        {
            // The metric hasn't been seen at all - report an empty window rather than nothing, so the feed
            // reads as "wired, no traffic observed" instead of "no feed" (which a null return would mean).
            return new MeshUsage(end, start, end, Array.Empty<MeshUsageEntry>());
        }

        // One Sum query per live dimension combination; remember each id's dimensions to map the result back.
        var byId = new Dictionary<string, Metric>();
        var queries = new List<MetricDataQuery>(metrics.Count);
        var index = 0;
        foreach (var metric in metrics)
        {
            var id = "m" + index++;
            byId[id] = metric;
            queries.Add(new MetricDataQuery
            {
                Id = id,
                ReturnData = true,
                MetricStat = new MetricStat
                {
                    Metric = new Metric
                    {
                        Namespace = _options.Namespace,
                        MetricName = _options.MetricName,
                        Dimensions = metric.Dimensions,
                    },
                    Period = _options.PeriodSeconds,
                    Stat = "Sum",
                },
            });
        }

        var entries = new List<MeshUsageEntry>();
        foreach (var chunk in Chunk(queries, MaxQueriesPerCall))
        {
            foreach (var result in await GetMetricDataAsync(chunk, start.UtcDateTime, end.UtcDateTime, cancellationToken))
            {
                if (!byId.TryGetValue(result.Id, out var metric))
                {
                    continue;
                }

                // Sum every bucket in the window - the total request count at this dimension combination.
                var count = (long)Math.Round((result.Values ?? new List<double>()).Sum());
                if (count <= 0)
                {
                    // In ListMetrics (seen at some point) but no traffic in this window - not "used" now.
                    continue;
                }

                var dimensions = ToLookup(metric.Dimensions);
                var topic = Get(dimensions, _options.TopicDimension);
                if (string.IsNullOrEmpty(topic))
                {
                    // Not one of ours (no topic dimension) - never guess.
                    continue;
                }

                entries.Add(new MeshUsageEntry(
                    topic: topic,
                    version: null,
                    service: null,
                    transport: Get(dimensions, _options.TransportDimension),
                    status: Get(dimensions, _options.ResultDimension),
                    count: count,
                    avgDurationMs: null,
                    source: MeshUsageSource.CloudWatch));
            }
        }

        return new MeshUsage(end, start, end, entries.ToArray());
    }

    private async Task<List<Metric>> ListMetricsAsync(CancellationToken cancellationToken)
    {
        var metrics = new List<Metric>();
        string? nextToken = null;
        do
        {
            var response = await _cloudWatch.ListMetricsAsync(new ListMetricsRequest
            {
                Namespace = _options.Namespace,
                MetricName = _options.MetricName,
                NextToken = nextToken,
            }, cancellationToken);

            if (response.Metrics != null)
            {
                metrics.AddRange(response.Metrics);
            }

            nextToken = response.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));

        return metrics;
    }

    private async Task<List<MetricDataResult>> GetMetricDataAsync(
        List<MetricDataQuery> queries, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken)
    {
        var results = new List<MetricDataResult>();
        string? nextToken = null;
        do
        {
            var response = await _cloudWatch.GetMetricDataAsync(new GetMetricDataRequest
            {
                MetricDataQueries = queries,
                StartTimeUtc = startUtc,
                EndTimeUtc = endUtc,
                NextToken = nextToken,
            }, cancellationToken);

            if (response.MetricDataResults != null)
            {
                results.AddRange(response.MetricDataResults);
            }

            nextToken = response.NextToken;
        }
        while (!string.IsNullOrEmpty(nextToken));

        return results;
    }

    private static IReadOnlyDictionary<string, string> ToLookup(List<Dimension>? dimensions)
    {
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dimension in dimensions ?? new List<Dimension>())
        {
            lookup[dimension.Name] = dimension.Value;
        }

        return lookup;
    }

    private static string? Get(IReadOnlyDictionary<string, string> dimensions, string name)
        => dimensions.TryGetValue(name, out var value) ? value : null;

    private static IEnumerable<List<T>> Chunk<T>(List<T> items, int size)
    {
        for (var i = 0; i < items.Count; i += size)
        {
            yield return items.GetRange(i, Math.Min(size, items.Count - i));
        }
    }
}
