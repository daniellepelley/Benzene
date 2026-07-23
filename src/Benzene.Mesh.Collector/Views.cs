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
}

/// <summary>The <c>mesh:query:fleet</c> request body (no parameters).</summary>
public class FleetQuery
{
}

/// <summary>The <c>mesh:query:service</c> request body.</summary>
public class ServiceQuery
{
    public string? Service { get; set; }
}

/// <summary>The <c>mesh:query:topic</c> request body.</summary>
public class TopicQuery
{
    public string? Topic { get; set; }
    public string? Version { get; set; }
}

/// <summary>The <c>mesh:query:trace</c> request body.</summary>
public class TraceQuery
{
    public string? TraceId { get; set; }
}

/// <summary>The <c>mesh:query:correlation</c> request body.</summary>
public class CorrelationQuery
{
    public string? CorrelationId { get; set; }
}
