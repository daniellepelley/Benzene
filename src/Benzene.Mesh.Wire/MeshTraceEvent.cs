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

/// <summary>The mesh wire-contract topic names (spec §1/§4), shared by services and collectors.</summary>
public static class MeshTopics
{
    /// <summary>The reserved descriptor topic a meshed service intercepts (spec §1).</summary>
    public const string Descriptor = "mesh";

    /// <summary>A service announces its descriptor to a collector (spec §4).</summary>
    public const string Register = "mesh:register";

    /// <summary>A service instance's periodic health report to a collector (spec §5).</summary>
    public const string Heartbeat = "mesh:heartbeat";

    /// <summary>A trace exporter's batched events to a collector (spec §4).</summary>
    public const string Traces = "mesh:traces";
}
