using System.Text.Json.Serialization;
using Benzene.Mesh.Wire;

namespace Benzene.Mesh.Collector;

/// <summary>Health classification of a service, from its instances' latest heartbeats.</summary>
public static class MeshHealth
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Unknown = "unknown";
}

/// <summary>Acknowledges an ingest message: how many items were accepted.</summary>
public class Ack
{
    public int Accepted { get; set; }
}

/// <summary>The <c>mesh:query:fleet</c> response: the whole known fleet in one shape.</summary>
public class FleetView
{
    public DateTimeOffset GeneratedAt { get; set; }
    public List<ServiceSummary> Services { get; set; } = new();
    public List<TopicSummary> Topics { get; set; } = new();
    public List<TraceSummary> Traces { get; set; } = new();

    /// <summary>The time window this view answers, when the query carried one. Absent (null) when the
    /// query carried no window - today's behavior, so old clients and fixtures see the field is not
    /// there. See <see cref="MeshWindow"/> for the flows-vs-counts honesty it carries.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeshWindow? Window { get; set; }
}

/// <summary>One service's fleet row. MissingFeeds names the feeds the collector has not received
/// for it ("descriptor", "health", "traces") - reduced is visible, never mistaken for empty.</summary>
public class ServiceSummary
{
    public string Service { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Runtime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Binding { get; set; }

    public MeshPlacement Placement { get; set; } = new();
    public int Topics { get; set; }
    public int Instances { get; set; }
    public string Health { get; set; } = MeshHealth.Unknown;
    public DateTimeOffset LastSeen { get; set; }
    public long Invocations { get; set; }
    public long Errors { get; set; }
    public List<string> MissingFeeds { get; set; } = new();
}

/// <summary>One topic's catalog row: providers from descriptors, consumers observed from trace
/// parentage, stats from the trace feed - nothing is declared.</summary>
public class TopicSummary
{
    public string Topic { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    public List<string> Providers { get; set; } = new();
    public List<string> Consumers { get; set; } = new();
    public long Invocations { get; set; }
    public long Errors { get; set; }
    public double AvgDurationMs { get; set; }
    public Dictionary<string, long> StatusCounts { get; set; } = new();
    public DateTimeOffset LastSeen { get; set; }

    /// <summary>Which stat dimensions are genuinely absent for this topic (the counts/duration are the
    /// non-nullable default, not an observed zero) - the same "reduced is visible, never mistaken for
    /// empty" degradation marker <see cref="ServiceSummary.MissingFeeds"/> carries, at the topic grain.
    /// Empty on the push-collector plane (it observes every dimension); a backend-composed reader that
    /// can't supply, e.g., duration names it here so the UI renders "—" not "0". Declared like
    /// <see cref="ServiceSummary.MissingFeeds"/> (always serialized, empty when nothing is missing); the
    /// fixtures' subset match ignores the extra key on the push-collector plane.</summary>
    public List<string> MissingFeeds { get; set; } = new();

    /// <summary>The time window this row answers, populated only on the standalone <c>mesh:query:topic</c>
    /// response when the query carried a window; omitted (null) when this summary is embedded in a
    /// <see cref="FleetView"/> (the fleet's <see cref="FleetView.Window"/> carries the one window for the
    /// whole view) and when no window was requested. See <see cref="MeshWindow"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeshWindow? Window { get; set; }
}

/// <summary>One recent flow on the fleet view.</summary>
public class TraceSummary
{
    public string TraceId { get; set; } = string.Empty;
    public int Events { get; set; }
    public List<string> Services { get; set; } = new();
    public DateTimeOffset StartedAt { get; set; }
    public double DurationMs { get; set; }
    public bool Failed { get; set; }
}

/// <summary>The <c>mesh:query:service</c> response: the fleet row plus the registered descriptor
/// and per-instance heartbeat state. HashMatches is false when an instance runs a different
/// contract than the collector knows (a redeploy it hasn't re-learned), null when either side
/// didn't supply a hash.</summary>
public class ServiceView
{
    public string Service { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Runtime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Binding { get; set; }

    public MeshPlacement Placement { get; set; } = new();
    public int Topics { get; set; }
    public string Health { get; set; } = MeshHealth.Unknown;
    public DateTimeOffset LastSeen { get; set; }
    public long Invocations { get; set; }
    public long Errors { get; set; }
    public List<string> MissingFeeds { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeshServiceDescriptor? Descriptor { get; set; }

    public List<InstanceView> Instances { get; set; } = new();

    /// <summary>The time window this view answers, when the query carried one; absent otherwise. See
    /// <see cref="MeshWindow"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeshWindow? Window { get; set; }
}

/// <summary>One instance's latest heartbeat state.</summary>
public class InstanceView
{
    public string InstanceId { get; set; } = string.Empty;
    public bool Healthy { get; set; }
    public DateTimeOffset LastHeartbeat { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DescriptorHash { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HashMatches { get; set; }
}

/// <summary>The <c>mesh:query:trace</c> response: the flow's events in start order.</summary>
public class TraceView
{
    public string TraceId { get; set; } = string.Empty;
    public List<MeshTraceEvent> Events { get; set; } = new();
}

/// <summary>The <c>mesh:query:correlation</c> response: every flow that carried a business
/// correlation id, grouped by trace. A correlation id is a business identifier that can span more
/// than one trace (several distinct flows sharing it), so this returns one <see cref="TraceView"/>
/// per matching trace rather than a single flattened event list - each group renders through the
/// same waterfall as a normal trace, and the "several distinct flows" distinction is preserved.</summary>
public class CorrelationView
{
    public string CorrelationId { get; set; } = string.Empty;
    public List<TraceView> Traces { get; set; } = new();

    /// <summary>The time window the search covered, when the query carried one; absent otherwise. See
    /// <see cref="MeshWindow"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeshWindow? Window { get; set; }
}

/// <summary>A requested query time range, in Grafana relative-time grammar (<c>now</c>, <c>now-5m</c>,
/// <c>now-1h</c>, <c>now-7d</c>, units s/m/h/d/w/M/y) or ISO-8601 absolute. Additive and optional on the
/// query bodies that carry it: a null range (or a range with no <see cref="From"/>) means "unfiltered" -
/// exactly today's behavior, so old clients and the conformance fixtures are unaffected. The default 1h
/// window is a UI-picker default, deliberately NOT a wire default: the wire never silently hides
/// pre-window flows. Resolved to absolute against the server's <c>now</c> at query time
/// (<see cref="MeshTimeRangeResolver"/>).</summary>
public class MeshTimeRange
{
    /// <summary>Lower bound - Grafana relative (<c>now-1h</c>) or ISO-8601 absolute. Null/empty ⇒ unfiltered.</summary>
    public string? From { get; set; }

    /// <summary>Upper bound - same grammar. Null/empty ⇒ <c>now</c>.</summary>
    public string? To { get; set; }
}

/// <summary>The resolved time window a read model answers, and - crucially - whether the row <em>counts</em>
/// honor it. The <see cref="From"/>/<see cref="To"/> bounds always describe the window applied to the view's
/// <em>flows</em> (recent-flows / correlation), which every plane can filter. The counts are the subtle part:
/// <list type="bullet">
/// <item>Push-collector plane: per-topic/service invocation counters are <em>cumulative since process
/// start</em> and can't be sub-windowed - <see cref="CountsWindowed"/> is false, <see cref="CountsSince"/> is
/// the collector's start.</item>
/// <item>Composite plane (X-Ray traces + a usage feed): flows honor the picked window, but the counts come
/// from the usage feed's own baked window (the CloudWatch/App-Insights adapters are single-window by design -
/// picker-driven usage windowing is a documented fast-follow), so <see cref="CountsWindowed"/> is false and
/// <see cref="CountsSince"/> is the usage feed's window start.</item>
/// </list>
/// A windowed count that can't honor the window is not "absent" (that's the
/// <see cref="TopicSummary.MissingFeeds"/> "—" channel, for a dimension genuinely not produced) - it's a real
/// number answering a different window, so the UI shows it with a "counts cover from {CountsSince}" badge,
/// never blanked. <see cref="CountsWindowed"/> is the seam that flips to true once a plane's counts honor the
/// picked window end-to-end.</summary>
public class MeshWindow
{
    /// <summary>Resolved absolute (ISO-8601) lower bound applied to the flows in this view.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Resolved absolute (ISO-8601) upper bound (the server's <c>now</c> unless a <c>To</c> was given).</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>True when the row counts honor <c>[From,To]</c>; false when the counts cover a different window
    /// (cumulative-since-start on the collector plane, or the usage feed's baked window on the composite plane)
    /// and the picked window applies to flows only.</summary>
    public bool CountsWindowed { get; set; }

    /// <summary>When <see cref="CountsWindowed"/> is false, the ISO-8601 instant the row counts actually cover
    /// from; null when <see cref="CountsWindowed"/> is true (the counts cover <see cref="From"/>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CountsSince { get; set; }
}

/// <summary>The <c>mesh:query:fleet</c> request body.</summary>
public class FleetQuery
{
    /// <summary>Optional query time range (additive; null ⇒ unfiltered, today's behavior).</summary>
    public MeshTimeRange? Window { get; set; }
}

/// <summary>The <c>mesh:query:service</c> request body.</summary>
public class ServiceQuery
{
    public string? Service { get; set; }

    /// <summary>Optional query time range (additive; null ⇒ unfiltered, today's behavior).</summary>
    public MeshTimeRange? Window { get; set; }
}

/// <summary>The <c>mesh:query:topic</c> request body.</summary>
public class TopicQuery
{
    public string? Topic { get; set; }
    public string? Version { get; set; }

    /// <summary>Optional query time range (additive; null ⇒ unfiltered, today's behavior).</summary>
    public MeshTimeRange? Window { get; set; }
}

/// <summary>The <c>mesh:query:trace</c> request body. Deliberately carries no window - a trace lookup is by
/// id, and a window on it would only let a valid id outside the range answer <c>NotFound</c>.</summary>
public class TraceQuery
{
    public string? TraceId { get; set; }
}

/// <summary>The <c>mesh:query:correlation</c> request body.</summary>
public class CorrelationQuery
{
    public string? CorrelationId { get; set; }

    /// <summary>Optional query time range (additive; null ⇒ unfiltered, today's behavior).</summary>
    public MeshTimeRange? Window { get; set; }
}
