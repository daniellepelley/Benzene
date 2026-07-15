namespace Benzene.Mesh.Tracing.Tempo;

/// <summary>
/// Configures where and over what time window <see cref="TempoServiceGraphTopologyBuilder"/>
/// queries Tempo's service-graph metrics.
/// </summary>
public class TempoTopologyOptions
{
    /// <summary>Initializes a new instance of the <see cref="TempoTopologyOptions"/> class.</summary>
    /// <param name="prometheusUrl">
    /// The Prometheus-compatible instant-query endpoint Tempo's metrics-generator remote-writes to
    /// (e.g. <c>http://prometheus:9090/api/v1/query</c>).
    /// </param>
    /// <param name="timeWindow">
    /// The lookback window used in each PromQL <c>rate(...)</c> query. Defaults to 5 minutes.
    /// Longer windows smooth over gaps but react more slowly to real traffic changes.
    /// </param>
    public TempoTopologyOptions(string prometheusUrl, TimeSpan? timeWindow = null)
    {
        PrometheusUrl = prometheusUrl;
        TimeWindow = timeWindow ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>The Prometheus-compatible instant-query endpoint to query.</summary>
    public string PrometheusUrl { get; }

    /// <summary>The lookback window used in each PromQL <c>rate(...)</c> query.</summary>
    public TimeSpan TimeWindow { get; }
}
