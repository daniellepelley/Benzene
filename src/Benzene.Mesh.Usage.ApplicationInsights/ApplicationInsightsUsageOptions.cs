namespace Benzene.Mesh.Usage.ApplicationInsights;

/// <summary>
/// Configures which Application Insights / Log Analytics workspace <see cref="ApplicationInsightsUsageSource"/>
/// queries, which metric, and over what window, to build the mesh usage feed. The defaults match the
/// <c>benzene.messages.processed</c> counter (tags <c>topic</c>/<c>transport</c>/<c>result</c>) as exported
/// to Azure Monitor by the Azure Monitor OpenTelemetry exporter - see <c>docs/mesh-usage-feed.md</c> and the
/// package's CLAUDE.md.
/// </summary>
public class ApplicationInsightsUsageOptions
{
    /// <summary>Initializes a new instance of the <see cref="ApplicationInsightsUsageOptions"/> class.</summary>
    /// <param name="workspaceId">
    /// The Log Analytics workspace id (GUID) backing the Application Insights resource the counter was
    /// exported to. This is the <c>customerId</c>/workspace id, not the App Insights instrumentation key.
    /// </param>
    /// <param name="timeWindow">
    /// The lookback window the counts cover, and the window surfaced on the Mesh UI (its start/end).
    /// Defaults to 24 hours. A longer window shows more cumulative usage; a shorter one reacts faster.
    /// </param>
    /// <param name="metricName">The counter's metric name. Defaults to <c>"benzene.messages.processed"</c>.</param>
    public ApplicationInsightsUsageOptions(
        string workspaceId,
        TimeSpan? timeWindow = null,
        string metricName = "benzene.messages.processed")
    {
        WorkspaceId = workspaceId;
        TimeWindow = timeWindow ?? TimeSpan.FromHours(24);
        MetricName = metricName;
    }

    /// <summary>The Log Analytics workspace id (GUID) to query.</summary>
    public string WorkspaceId { get; }

    /// <summary>The counter's metric name (the <c>customMetrics</c> <c>name</c>).</summary>
    public string MetricName { get; }

    /// <summary>The lookback window the counts cover (also the window surfaced on the Mesh UI).</summary>
    public TimeSpan TimeWindow { get; }

    /// <summary>The <c>customDimensions</c> key carrying the topic id. Defaults to <c>"topic"</c> (the counter's tag key).</summary>
    public string TopicDimension { get; set; } = "topic";

    /// <summary>The <c>customDimensions</c> key carrying the transport name. Defaults to <c>"transport"</c>.</summary>
    public string TransportDimension { get; set; } = "transport";

    /// <summary>The <c>customDimensions</c> key carrying the result/outcome class. Defaults to <c>"result"</c> (values <c>success</c>/<c>failure</c>).</summary>
    public string ResultDimension { get; set; } = "result";
}
