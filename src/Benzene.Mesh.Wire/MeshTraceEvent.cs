using System.Text.Json.Serialization;
using Benzene.HealthChecks.Core;

namespace Benzene.Mesh.Wire;

/// <summary>
/// One pipeline invocation as the mesh sees it (docs/specification/mesh.md §3) - semantic
/// (topic + Benzene status), not transport-shaped. Trace ids are the W3C Trace Context fields.
/// </summary>
public class MeshTraceEvent
{
    public string TraceId { get; set; } = string.Empty;

    public string SpanId { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentSpanId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Service { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InstanceId { get; set; }

    public string Topic { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TopicVersion { get; set; }

    /// <summary>The Benzene status verbatim; empty only when no downstream middleware produced a result.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>When the failure originated in a thrown exception, the exception's type name (spec §3,
    /// optional/additive) — the stable non-sensitive discriminator, never the message or stack trace.
    /// Null for non-exception failures or when the emitter didn't capture it.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExceptionType { get; set; }

    public double DurationMs { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CorrelationId { get; set; }
}

/// <summary>The body of a <c>mesh:traces</c> message (spec §4): one exporter flush.</summary>
public class MeshTraceBatch
{
    public List<MeshTraceEvent> Events { get; set; } = new();
}

/// <summary>
/// The body of a <c>mesh:heartbeat</c> message (spec §5): the standard aggregate health response
/// reused as-is, wrapped with identity and the contract hash.
/// </summary>
public class MeshHeartbeat
{
    public string Service { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InstanceId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DescriptorHash { get; set; }

    public DateTimeOffset SentAt { get; set; }

    public HealthCheckResponse? Health { get; set; }
}

/// <summary>
/// The mesh wire-contract topic names (spec §1/§4), shared by services and collectors.
/// <para>
/// All ids carry the <c>benzene:</c> marker per the naming principle
/// (<c>work/benzene-naming-principle.md</c>) — they live in the same namespace as the
/// application's topics, so they say whose they are. They sit here rather than on
/// <see cref="Benzene.Abstractions.BenzeneTopic"/> because the mesh is an optional add-on and the
/// root abstraction deliberately doesn't know about it; <c>BenzeneTopic.IsReserved</c> still
/// recognises them, because it tests the prefix rather than a list.
/// </para>
/// </summary>
public static class MeshTopics
{
    /// <summary>The reserved descriptor topic a meshed service intercepts (spec §1).</summary>
    public const string Descriptor = Benzene.Abstractions.BenzeneTopic.Mesh;

    /// <summary>A service announces its descriptor to a collector (spec §4).</summary>
    public const string Register = "benzene:mesh:register";

    /// <summary>A service instance's periodic health report to a collector (spec §5).</summary>
    public const string Heartbeat = "benzene:mesh:heartbeat";

    /// <summary>A trace exporter's batched events to a collector (spec §4).</summary>
    public const string Traces = "benzene:mesh:traces";

    /// <summary>An issue emitter's deduplicated failure signatures to a collector (spec §4.1).</summary>
    public const string Issues = "benzene:mesh:issues";

    // Host-side operational topics (aggregate/report/annotations/dispatch/topology) are NOT here:
    // they are not part of the cross-service wire contract, and their packages don't reference this
    // one. Each lives in a constants class in its own package, all carrying the same benzene: marker.

    /// <summary>Reads the whole known fleet (services, topics, recent flows).</summary>
    public const string QueryFleet = "benzene:mesh:query:fleet";

    /// <summary>Reads one service's detail.</summary>
    public const string QueryService = "benzene:mesh:query:service";

    /// <summary>Reads one topic's summary.</summary>
    public const string QueryTopic = "benzene:mesh:query:topic";

    /// <summary>Reads one flow's traced waterfall by trace id.</summary>
    public const string QueryTrace = "benzene:mesh:query:trace";

    /// <summary>Reads every flow carrying a business correlation id.</summary>
    public const string QueryCorrelation = "benzene:mesh:query:correlation";
}
