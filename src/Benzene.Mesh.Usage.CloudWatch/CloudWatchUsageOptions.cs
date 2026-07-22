namespace Benzene.Mesh.Usage.CloudWatch;

/// <summary>
/// Configures which CloudWatch metric <see cref="CloudWatchUsageSource"/> reads, and over what window,
/// to build the mesh usage feed. The defaults match the <c>benzene.messages.processed</c> counter
/// (tags <c>topic</c>/<c>transport</c>/<c>result</c>) as exported to CloudWatch by the ADOT collector's
/// EMF exporter - see <c>docs/mesh-usage-feed.md</c> and the package's CLAUDE.md.
/// </summary>
public class CloudWatchUsageOptions
{
    /// <summary>Initializes a new instance of the <see cref="CloudWatchUsageOptions"/> class.</summary>
    /// <param name="namespace">
    /// The CloudWatch namespace the counter was exported to (the EMF exporter's <c>namespace</c>).
    /// Defaults to <c>"Benzene/Mesh"</c>.
    /// </param>
    /// <param name="timeWindow">
    /// The lookback window the counts cover, and the window surfaced on the Mesh UI (its start/end).
    /// Defaults to 24 hours. A longer window shows more cumulative usage; a shorter one reacts faster.
    /// </param>
    /// <param name="metricName">The counter's metric name. Defaults to <c>"benzene.messages.processed"</c>.</param>
    public CloudWatchUsageOptions(
        string @namespace = "Benzene/Mesh",
        TimeSpan? timeWindow = null,
        string metricName = "benzene.messages.processed")
    {
        Namespace = @namespace;
        TimeWindow = timeWindow ?? TimeSpan.FromHours(24);
        MetricName = metricName;
    }

    /// <summary>The CloudWatch namespace the counter was exported to.</summary>
    public string Namespace { get; }

    /// <summary>The counter's metric name.</summary>
    public string MetricName { get; }

    /// <summary>The lookback window the counts cover (also the window surfaced on the Mesh UI).</summary>
    public TimeSpan TimeWindow { get; }

    /// <summary>The metric dimension carrying the topic id. Defaults to <c>"topic"</c> (the counter's tag key).</summary>
    public string TopicDimension { get; set; } = "topic";

    /// <summary>The metric dimension carrying the transport name. Defaults to <c>"transport"</c>.</summary>
    public string TransportDimension { get; set; } = "transport";

    /// <summary>The metric dimension carrying the result/outcome class. Defaults to <c>"result"</c> (values <c>success</c>/<c>failure</c>).</summary>
    public string ResultDimension { get; set; } = "result";

    /// <summary>
    /// The bucket period, in seconds, each <c>Sum</c> query uses; the source sums every bucket in the
    /// window, so this only affects resolution/cost, not the total. Defaults to 60 (CloudWatch's standard
    /// resolution for a custom metric). Must be a positive multiple of 60.
    /// </summary>
    public int PeriodSeconds { get; set; } = 60;
}
