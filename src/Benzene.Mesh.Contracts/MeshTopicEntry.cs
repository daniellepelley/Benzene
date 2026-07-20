using System.Text.Json.Nodes;

namespace Benzene.Mesh.Contracts;

/// <summary>
/// One distinct (topic, version) pair in a <see cref="MeshTopicCatalog"/>, and every service in
/// the fleet that produces or consumes it. Aggregator-computed output, derived entirely from each
/// service's own self-description (spec <c>requests</c>/<c>events</c>) — never a claim any single
/// service makes about itself. See <see cref="Status"/>.
/// </summary>
public class MeshTopicEntry
{
    /// <summary>Initializes a new instance of the <see cref="MeshTopicEntry"/> class.</summary>
    /// <param name="topic">The topic id.</param>
    /// <param name="version">The topic's handler version (empty for the unversioned handler).</param>
    /// <param name="reserved">True when this is a reserved Benzene utility topic (spec/health/mesh/…) rather than a domain topic.</param>
    /// <param name="consumers">The services that handle this topic (spec <c>requests</c>).</param>
    /// <param name="producers">The services that declare sending this topic (spec <c>events</c>).</param>
    /// <param name="status">One of <see cref="MeshTopicStatus"/>, or <c>null</c> when neither signal applies — see <see cref="Status"/>.</param>
    /// <param name="requestSchema">The inbound payload's JSON Schema (from a consumer's spec), <c>$ref</c>s inlined; <c>null</c> when unknown. See <see cref="RequestSchema"/>.</param>
    /// <param name="responseSchema">The response payload's JSON Schema (from a consumer's spec), <c>$ref</c>s inlined; <c>null</c> when unknown.</param>
    /// <param name="messageSchema">The broadcast message's JSON Schema (from a producer's spec <c>events</c>), <c>$ref</c>s inlined; <c>null</c> when unknown.</param>
    /// <param name="schemaMismatch">True when the topic's consumers do not all declare the same inbound payload — likely a contract error. See <see cref="SchemaMismatch"/>.</param>
    public MeshTopicEntry(string topic, string version, bool reserved,
        MeshTopicService[] consumers, MeshTopicProducer[] producers, string? status,
        JsonObject? requestSchema = null, JsonObject? responseSchema = null, JsonObject? messageSchema = null,
        bool schemaMismatch = false)
    {
        Topic = topic;
        Version = version;
        Reserved = reserved;
        Consumers = consumers;
        Producers = producers;
        Status = status;
        RequestSchema = requestSchema;
        ResponseSchema = responseSchema;
        MessageSchema = messageSchema;
        SchemaMismatch = schemaMismatch;
    }

    /// <summary>The topic id.</summary>
    public string Topic { get; }

    /// <summary>The topic's handler version (empty for the unversioned handler).</summary>
    public string Version { get; }

    /// <summary>True when this is a reserved Benzene utility topic rather than a domain topic.</summary>
    public bool Reserved { get; }

    /// <summary>The services that handle this topic (spec <c>requests</c>).</summary>
    public MeshTopicService[] Consumers { get; }

    /// <summary>The services that declare sending this topic (spec <c>events</c>).</summary>
    public MeshTopicProducer[] Producers { get; }

    /// <summary>
    /// An informational signal computed from <see cref="Producers"/>/<see cref="Consumers"/> —
    /// never present on a reserved topic (a health check has no "producer" in this sense).
    /// One of <see cref="MeshTopicStatus"/>, or <c>null</c> when the topic looks ordinary (has
    /// both producers and consumers, or is a plain HTTP-invoked endpoint with no fleet-internal
    /// producer expected in the first place). Neither non-null value is an error: a
    /// <see cref="MeshTopicStatus.DeprecationCandidate"/> topic may simply not have been retired
    /// yet, and a <see cref="MeshTopicStatus.Gap"/> topic may legitimately be fed by a third party
    /// or a non-Benzene system outside this fleet.
    /// </summary>
    public string? Status { get; }

    /// <summary>
    /// The inbound payload's JSON Schema for this (topic, version), lifted from a consumer's spec
    /// <c>requests</c> entry with its <c>$ref</c>s to <c>components.schemas</c> inlined so it is
    /// self-contained (recursive types are cut with a <c>title</c> marker). <c>null</c> when no
    /// consumer's spec carried a schema. When <see cref="SchemaMismatch"/> is true this is one
    /// consumer's schema, not a merge.
    /// </summary>
    public JsonObject? RequestSchema { get; }

    /// <summary>
    /// The response payload's JSON Schema for this (topic, version), from a consumer's spec, with
    /// <c>$ref</c>s inlined. <c>null</c> for one-way topics or when unknown.
    /// </summary>
    public JsonObject? ResponseSchema { get; }

    /// <summary>
    /// The broadcast message's JSON Schema for this (topic, version), from a producer's spec
    /// <c>events</c> entry, with <c>$ref</c>s inlined. <c>null</c> when no producer's spec carried
    /// a schema.
    /// </summary>
    public JsonObject? MessageSchema { get; }

    /// <summary>
    /// True when two or more consumers of this exact (topic, version) declare <em>different</em>
    /// inbound payload schemas. Handlers of the same topic and version are expected to agree on the
    /// payload, so this is surfaced as a likely contract error for someone to reconcile — never a
    /// benign informational signal like <see cref="Status"/>. Computed by comparing the inlined,
    /// key-order-normalized request/response schemas across every consumer; never set for a reserved
    /// utility topic.
    /// </summary>
    public bool SchemaMismatch { get; }
}
