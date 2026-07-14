using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Tracing.Tempo;

/// <summary>
/// Builds a <see cref="MeshTopology"/> from Grafana Tempo's metrics-generator service-graph
/// metrics, queried via a Prometheus-compatible endpoint (see
/// <c>work/service-mesh-roadmap-1.0.md</c> §4.6.1 for why this is a PromQL client rather than a
/// Tempo trace-API client).
/// </summary>
/// <remarks>
/// An edge only appears in the result if it has at least one matching timeseries in the queried
/// window - real Prometheus semantics, not a bug: a client/server pair with no traffic in the
/// window produces no timeseries at all, so there is nothing to report an edge for.
/// </remarks>
public class TempoServiceGraphTopologyBuilder
{
    private const string RequestTotalMetric = "traces_service_graph_request_total";
    private const string RequestFailedMetric = "traces_service_graph_request_failed_total";
    private const string ServerSecondsBucketMetric = "traces_service_graph_request_server_seconds_bucket";

    private readonly PrometheusQueryClient _client;
    private readonly TempoTopologyOptions _options;
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>Initializes a new instance of the <see cref="TempoServiceGraphTopologyBuilder"/> class.</summary>
    /// <param name="client">The Prometheus query client to use.</param>
    /// <param name="options">Where and over what window to query.</param>
    /// <param name="clock">Supplies the current time; defaults to <see cref="DateTimeOffset.UtcNow"/>. Overridable for deterministic tests.</param>
    public TempoServiceGraphTopologyBuilder(PrometheusQueryClient client, TempoTopologyOptions options, Func<DateTimeOffset>? clock = null)
    {
        _client = client;
        _options = options;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>Queries Tempo's service-graph metrics and assembles the resulting edges.</summary>
    /// <returns>The built topology.</returns>
    public async Task<MeshTopology> BuildAsync()
    {
        var window = FormatPromQlDuration(_options.TimeWindow);

        var requestsPerMinute = await QueryPerMinuteAsync(RequestTotalMetric, window);
        var failedPerMinute = await QueryPerMinuteAsync(RequestFailedMetric, window);
        var p50 = await QueryLatencyMsAsync(0.50, window);
        var p95 = await QueryLatencyMsAsync(0.95, window);
        var p99 = await QueryLatencyMsAsync(0.99, window);

        var edgeKeys = new HashSet<(string Client, string Server)>();
        edgeKeys.UnionWith(requestsPerMinute.Keys);
        edgeKeys.UnionWith(failedPerMinute.Keys);
        edgeKeys.UnionWith(p50.Keys);
        edgeKeys.UnionWith(p95.Keys);
        edgeKeys.UnionWith(p99.Keys);

        var edges = edgeKeys.Select(key =>
        {
            double? requests = requestsPerMinute.TryGetValue(key, out var r) ? r : null;
            double? failed = failedPerMinute.TryGetValue(key, out var f) ? f : null;
            double? p50Value = p50.TryGetValue(key, out var v50) ? v50 : null;
            double? p95Value = p95.TryGetValue(key, out var v95) ? v95 : null;
            double? p99Value = p99.TryGetValue(key, out var v99) ? v99 : null;

            return new TopologyEdge(
                key.Client,
                key.Server,
                TopologyEdgeSource.Tempo,
                requests,
                ComputeErrorRate(requests, failed),
                p50Value,
                p95Value,
                p99Value);
        }).ToArray();

        return new MeshTopology(_clock(), edges);
    }

    private static double? ComputeErrorRate(double? requestsPerMinute, double? failedPerMinute)
    {
        if (failedPerMinute == null)
        {
            return requestsPerMinute == null ? null : 0d;
        }

        if (requestsPerMinute == null || requestsPerMinute == 0)
        {
            return 0d;
        }

        return failedPerMinute / requestsPerMinute;
    }

    private async Task<Dictionary<(string Client, string Server), double>> QueryPerMinuteAsync(string metric, string window)
    {
        var promQl = $"sum by (client, server) (rate({metric}[{window}])) * 60";
        var samples = await _client.QueryAsync(_options.PrometheusUrl, promQl);
        return ToEdgeMap(samples);
    }

    private async Task<Dictionary<(string Client, string Server), double>> QueryLatencyMsAsync(double quantile, string window)
    {
        var promQl = $"histogram_quantile({quantile.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}, " +
            $"sum by (le, client, server) (rate({ServerSecondsBucketMetric}[{window}]))) * 1000";
        var samples = await _client.QueryAsync(_options.PrometheusUrl, promQl);
        return ToEdgeMap(samples);
    }

    private static Dictionary<(string Client, string Server), double> ToEdgeMap(IReadOnlyList<PrometheusSample> samples)
    {
        var map = new Dictionary<(string, string), double>();
        foreach (var sample in samples)
        {
            if (!sample.Labels.TryGetValue("client", out var client) || !sample.Labels.TryGetValue("server", out var server))
            {
                continue;
            }

            if (double.IsNaN(sample.Value))
            {
                continue;
            }

            map[(client, server)] = sample.Value;
        }

        return map;
    }

    private static string FormatPromQlDuration(TimeSpan window)
    {
        if (window.TotalHours >= 1 && window.TotalHours == Math.Floor(window.TotalHours))
        {
            return $"{(int)window.TotalHours}h";
        }

        if (window.TotalMinutes == Math.Floor(window.TotalMinutes))
        {
            return $"{(int)window.TotalMinutes}m";
        }

        return $"{(int)window.TotalSeconds}s";
    }
}
