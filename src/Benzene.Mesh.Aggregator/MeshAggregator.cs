using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Benzene.HealthChecks.Core;
using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Polls every service in a <see cref="MeshServiceRegistry"/> for its spec and health documents,
/// computes contract-drift, and publishes the resulting catalog to an <see cref="IMeshArtifactStore"/>.
/// </summary>
public class MeshAggregator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    // Matches Benzene.HealthChecks.TimeOutHealthCheck's 10-second convention: an explicit,
    // documented bound on each fetch rather than relying solely on a source's own (potentially much
    // longer) defaults - one slow/hung service shouldn't be able to stall a run.
    private static readonly TimeSpan PerServiceFetchTimeout = TimeSpan.FromSeconds(10);

    private readonly IReadOnlyDictionary<string, IMeshServiceSource> _sources;
    private readonly IMeshArtifactStore _store;
    private readonly Func<DateTimeOffset> _clock;

    /// <summary>Initializes a new instance of the <see cref="MeshAggregator"/> class.</summary>
    /// <param name="sources">
    /// Every registered <see cref="IMeshServiceSource"/>, keyed by <see cref="IMeshServiceSource.Key"/>
    /// (case-insensitive) to resolve each entry's <see cref="MeshServiceRegistryEntry.Source"/>
    /// against. An entry whose <c>Source</c> has no matching source here is recorded as that
    /// service's own fetch error, not a run-wide failure.
    /// </param>
    /// <param name="store">Where generated catalog artifacts are published (and, for contract-drift comparison, read back from).</param>
    /// <param name="clock">Supplies the current time; defaults to <see cref="DateTimeOffset.UtcNow"/>. Overridable for deterministic tests.</param>
    public MeshAggregator(IEnumerable<IMeshServiceSource> sources, IMeshArtifactStore store, Func<DateTimeOffset>? clock = null)
    {
        _sources = sources.ToDictionary(source => source.Key, StringComparer.OrdinalIgnoreCase);
        _store = store;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Polls every registered service once, publishing a <c>services/{name}.json</c> snapshot per
    /// service and a top-level <c>manifest.json</c> summarizing all of them. A single service's
    /// spec/health fetch failing (or timing out - see <c>PerServiceFetchTimeout</c>) does not
    /// prevent the rest from being processed and published. Services are polled concurrently, not
    /// one at a time, so one slow service adds to the run's total time only up to its own timeout,
    /// not to every other service's fetch time as well - the same shape as
    /// <c>Benzene.HealthChecks.HealthCheckProcessor.PerformHealthChecksAsync</c>.
    /// </summary>
    /// <param name="registry">The services to poll.</param>
    /// <returns>The published manifest.</returns>
    public async Task<MeshManifest> RunOnceAsync(MeshServiceRegistry registry)
    {
        var entries = registry.Services;
        var results = await Task.WhenAll(entries.Select(BuildServiceAsync));

        var manifestEntries = new List<MeshManifestEntry>(entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var snapshot = results[i].Snapshot;
            await _store.PublishAsync($"services/{entry.Name}.json", JsonSerializer.Serialize(snapshot, JsonOptions));

            manifestEntries.Add(new MeshManifestEntry(
                entry.Name, DetermineStatus(snapshot), snapshot.ContractDrift, entry.SpecUrl, entry.HealthUrl,
                entry.OwningTeam, results[i].Transports.ToArray()));
        }

        var manifest = new MeshManifest(_clock(), manifestEntries.ToArray());
        await _store.PublishAsync("manifest.json", JsonSerializer.Serialize(manifest, JsonOptions));

        // Cross-service topic catalog: every topic across the mesh -> which service(s) expose it.
        var catalog = BuildTopicCatalog(entries, results);
        await _store.PublishAsync("topics.json", JsonSerializer.Serialize(catalog, JsonOptions));

        // Structural ("designed to call") topology: an edge from each service that *sends* a domain
        // topic to each service that *handles* it, derived from the specs (no tracing backend needed).
        var topology = BuildTopology(entries, results);
        await _store.PublishAsync("topology.json", JsonSerializer.Serialize(topology, JsonOptions));

        return manifest;
    }

    /// <summary>
    /// Derives the structural topology: for every domain topic a service declares it <em>sends</em>
    /// (the spec's <c>events</c>), an edge to every service that <em>handles</em> it (the spec's
    /// <c>requests</c>). This is <see cref="TopologyEdgeSource.Structural"/> — the "designed to call"
    /// graph, as opposed to <c>Benzene.Mesh.Tracing.Tempo</c>'s observed traffic.
    /// </summary>
    private MeshTopology BuildTopology(MeshServiceRegistryEntry[] entries, ServiceResult[] results)
    {
        var handlersByTopic = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        for (var i = 0; i < entries.Length; i++)
        {
            foreach (var topic in results[i].Topics.Where(t => !t.Reserved))
            {
                if (!handlersByTopic.TryGetValue(topic.Topic, out var handlers))
                {
                    handlers = new List<string>();
                    handlersByTopic[topic.Topic] = handlers;
                }
                handlers.Add(entries[i].Name);
            }
        }

        var edges = new List<TopologyEdge>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < entries.Length; i++)
        {
            var client = entries[i].Name;
            // Topology edges are topic-id-level, not version-level - a client calling any version
            // of a topic is structurally wired to whichever service(s) handle any version of it.
            foreach (var topic in results[i].OutboundTopics)
            {
                if (!handlersByTopic.TryGetValue(topic.Topic, out var servers))
                {
                    continue;
                }
                foreach (var server in servers)
                {
                    if (string.Equals(server, client, StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // a service calling itself isn't a mesh edge
                    }
                    if (seen.Add($"{client} {server}"))
                    {
                        edges.Add(new TopologyEdge(client, server, TopologyEdgeSource.Structural,
                            requestsPerMinute: null, errorRate: null,
                            p50LatencyMs: null, p95LatencyMs: null, p99LatencyMs: null));
                    }
                }
            }
        }

        return new MeshTopology(_clock(), edges.ToArray());
    }

    /// <summary>
    /// Builds the cross-service topic catalog: every (topic, version) pair seen anywhere in the
    /// fleet, who consumes it (spec <c>requests</c>) and who produces it (spec <c>events</c>), plus
    /// an informational <see cref="MeshTopicEntry.Status"/>. This is entirely aggregator-computed
    /// from what services self-describe — no service is ever asked, or able, to know this about
    /// itself; only looking across the whole fleet at once can answer it
    /// (work/service-mesh-roadmap-1.0.md §10.9).
    /// </summary>
    private MeshTopicCatalog BuildTopicCatalog(MeshServiceRegistryEntry[] entries, ServiceResult[] results)
    {
        var byTopic = new Dictionary<(string Topic, string Version), TopicAggregate>();
        for (var i = 0; i < entries.Length; i++)
        {
            foreach (var topic in results[i].Topics)
            {
                var key = (topic.Topic, topic.Version);
                if (!byTopic.TryGetValue(key, out var aggregate))
                {
                    aggregate = new TopicAggregate();
                    byTopic[key] = aggregate;
                }

                aggregate.Reserved |= topic.Reserved;
                aggregate.Consumers.Add(new MeshTopicService(entries[i].Name, topic.HttpMappings));
                aggregate.ConsumerSchemas.Add((topic.RequestSchema, topic.ResponseSchema));
            }

            foreach (var outbound in results[i].OutboundTopics)
            {
                var key = (outbound.Topic, outbound.Version);
                if (!byTopic.TryGetValue(key, out var aggregate))
                {
                    aggregate = new TopicAggregate();
                    byTopic[key] = aggregate;
                }

                aggregate.Producers.Add(new MeshTopicProducer(entries[i].Name));
                aggregate.MessageSchema ??= outbound.MessageSchema;
            }
        }

        var topics = byTopic
            .Select(kvp => BuildTopicEntry(kvp.Key.Topic, kvp.Key.Version, kvp.Value))
            .OrderBy(x => x.Reserved) // domain topics first, utilities last
            .ThenBy(x => x.Topic, StringComparer.Ordinal)
            .ThenBy(x => x.Version, StringComparer.Ordinal)
            .ToArray();

        return new MeshTopicCatalog(_clock(), topics);
    }

    /// <summary>
    /// A reserved topic never gets a status - a health check has no "producer" in this sense, so
    /// the absence of one is not informative. For a domain topic: producers declared but nobody
    /// consumes it is a deprecation candidate; consumers exist but nobody in the fleet produces it
    /// AND none of those consumers are HTTP-reachable is a gap (an HTTP-invoked topic's "producer"
    /// is inherently an external caller - a browser, a third party - never a fleet-internal spec
    /// declaration, so that case alone would otherwise flag on nearly every ordinary REST
    /// endpoint). Anything else (both sides present, or an HTTP endpoint with no consumers-side
    /// signal to read) is left unflagged rather than guessed at.
    /// </summary>
    private static string? DetermineTopicStatus(TopicAggregate aggregate)
    {
        if (aggregate.Reserved)
        {
            return null;
        }

        var hasProducers = aggregate.Producers.Count > 0;
        var hasConsumers = aggregate.Consumers.Count > 0;

        if (hasProducers && !hasConsumers)
        {
            return MeshTopicStatus.DeprecationCandidate;
        }

        if (!hasProducers && hasConsumers && aggregate.Consumers.All(c => c.HttpMappings.Length == 0))
        {
            return MeshTopicStatus.Gap;
        }

        return null;
    }

    /// <summary>
    /// Assembles one <see cref="MeshTopicEntry"/> from a topic's aggregate: the representative payload
    /// schemas (first consumer's request/response, first producer's message) plus the cross-consumer
    /// <see cref="MeshTopicEntry.SchemaMismatch"/> flag - two consumers of the same (topic, version)
    /// declaring different inbound payloads is a likely contract error, so it is compared here (over
    /// the inlined, key-order-normalized schemas) and surfaced, never on a reserved utility topic.
    /// </summary>
    private static MeshTopicEntry BuildTopicEntry(string topic, string version, TopicAggregate aggregate)
    {
        var requestSchema = aggregate.ConsumerSchemas.Select(pair => pair.Request).FirstOrDefault(schema => schema != null);
        var responseSchema = aggregate.ConsumerSchemas.Select(pair => pair.Response).FirstOrDefault(schema => schema != null);

        // Only consumers that actually declared a request schema are compared - a consumer whose spec
        // predates schema-in-spec contributes no schema rather than a spurious "differs from" signal.
        var mismatch = !aggregate.Reserved && aggregate.ConsumerSchemas
            .Where(pair => pair.Request != null)
            .Select(pair => Canonical(pair.Request) + " " + Canonical(pair.Response))
            .Distinct(StringComparer.Ordinal)
            .Count() > 1;

        return new MeshTopicEntry(
            topic, version, aggregate.Reserved,
            aggregate.Consumers.ToArray(), aggregate.Producers.ToArray(),
            DetermineTopicStatus(aggregate),
            requestSchema, responseSchema, aggregate.MessageSchema, mismatch);
    }

    private sealed class TopicAggregate
    {
        public bool Reserved;
        public readonly List<MeshTopicService> Consumers = new();
        public readonly List<MeshTopicProducer> Producers = new();
        public readonly List<(JsonObject? Request, JsonObject? Response)> ConsumerSchemas = new();
        public JsonObject? MessageSchema;
    }

    private async Task<ServiceResult> BuildServiceAsync(MeshServiceRegistryEntry entry)
    {
        var source = ResolveSource(entry.Source);

        string? specJson = null;
        string? error = null;

        try
        {
            using var specTimeout = new CancellationTokenSource(PerServiceFetchTimeout);
            specJson = await source.FetchSpecAsync(entry, specTimeout.Token);
        }
        catch (Exception ex)
        {
            // Type name only, never the message - this artifact aggregates across services into
            // something with broader visibility than one service's own health endpoint (same
            // posture as the Data["Error"] fix across the HealthChecks family). A timeout surfaces
            // here as TaskCanceledException, same as any other fetch failure.
            error = ex.GetType().Name;
        }

        HealthCheckResponse? health = null;
        try
        {
            using var healthTimeout = new CancellationTokenSource(PerServiceFetchTimeout);
            var healthJson = await source.FetchHealthAsync(entry, healthTimeout.Token);
            health = JsonSerializer.Deserialize<HealthCheckResponse>(healthJson, JsonOptions);
        }
        catch (Exception ex)
        {
            error ??= ex.GetType().Name;
        }

        var snapshot = await MeshSnapshotBuilder.BuildAsync(_store, entry.Name, _clock(), specJson, health, error);
        return new ServiceResult(snapshot, ParseTopics(specJson), ParseOutboundTopics(specJson), ParseTransports(specJson));
    }

    private readonly record struct ServiceResult(
        MeshServiceSnapshot Snapshot, IReadOnlyList<ServiceTopic> Topics, IReadOnlyList<ServiceOutboundTopic> OutboundTopics,
        IReadOnlyList<string> Transports);

    private readonly record struct ServiceTopic(string Topic, string Version, bool Reserved, MeshTopicHttpMapping[] HttpMappings,
        JsonObject? RequestSchema, JsonObject? ResponseSchema);

    private readonly record struct ServiceOutboundTopic(string Topic, string Version, JsonObject? MessageSchema);

    /// <summary>
    /// Extracts the topics from a service's <c>benzene</c> spec (its <c>requests</c> array) for the
    /// cross-service topic catalog. Best-effort: a missing/unparseable spec contributes no topics
    /// (the service is still catalogued via its snapshot), never failing the run.
    /// </summary>
    private static IReadOnlyList<ServiceTopic> ParseTopics(string? specJson)
    {
        if (string.IsNullOrWhiteSpace(specJson))
        {
            return Array.Empty<ServiceTopic>();
        }

        try
        {
            using var doc = JsonDocument.Parse(specJson);
            if (!doc.RootElement.TryGetProperty("requests", out var requests) || requests.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<ServiceTopic>();
            }

            var components = ReadComponents(doc.RootElement);
            var topics = new List<ServiceTopic>();
            foreach (var request in requests.EnumerateArray())
            {
                if (!request.TryGetProperty("topic", out var topicElement) || topicElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var version = request.TryGetProperty("version", out var versionElement) && versionElement.ValueKind == JsonValueKind.String
                    ? versionElement.GetString() ?? ""
                    : "";

                var reserved = request.TryGetProperty("reserved", out var reservedElement)
                               && reservedElement.ValueKind == JsonValueKind.True;

                var mappings = new List<MeshTopicHttpMapping>();
                if (request.TryGetProperty("httpMappings", out var httpMappings) && httpMappings.ValueKind == JsonValueKind.Array)
                {
                    foreach (var mapping in httpMappings.EnumerateArray())
                    {
                        var method = mapping.TryGetProperty("method", out var m) ? m.GetString() ?? "" : "";
                        var path = mapping.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
                        mappings.Add(new MeshTopicHttpMapping(method, path));
                    }
                }

                topics.Add(new ServiceTopic(topicElement.GetString()!, version, reserved, mappings.ToArray(),
                    ExtractSchema(request, "request", components), ExtractSchema(request, "response", components)));
            }

            return topics;
        }
        catch (JsonException)
        {
            return Array.Empty<ServiceTopic>();
        }
    }

    /// <summary>
    /// Extracts the topics a service declares it <em>sends</em> from its <c>benzene</c> spec (the
    /// <c>events</c> array — broadcast/sender declarations), for structural topology derivation and
    /// the topic catalog's producer side. Best-effort, same posture as <see cref="ParseTopics"/>.
    /// </summary>
    private static IReadOnlyList<ServiceOutboundTopic> ParseOutboundTopics(string? specJson)
    {
        if (string.IsNullOrWhiteSpace(specJson))
        {
            return Array.Empty<ServiceOutboundTopic>();
        }

        try
        {
            using var doc = JsonDocument.Parse(specJson);
            if (!doc.RootElement.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<ServiceOutboundTopic>();
            }

            var components = ReadComponents(doc.RootElement);
            var topics = new List<ServiceOutboundTopic>();
            foreach (var @event in events.EnumerateArray())
            {
                if (@event.TryGetProperty("topic", out var topic) && topic.ValueKind == JsonValueKind.String)
                {
                    var version = @event.TryGetProperty("version", out var versionElement) && versionElement.ValueKind == JsonValueKind.String
                        ? versionElement.GetString() ?? ""
                        : "";
                    topics.Add(new ServiceOutboundTopic(topic.GetString()!, version, ExtractSchema(@event, "message", components)));
                }
            }

            return topics;
        }
        catch (JsonException)
        {
            return Array.Empty<ServiceOutboundTopic>();
        }
    }

    /// <summary>
    /// Extracts the document-level <c>transports</c> field from a service's <c>benzene</c> spec -
    /// every transport that service is wired to receive messages over. Best-effort, same posture
    /// as <see cref="ParseTopics"/>: a missing/unparseable spec, or one from before this field
    /// existed, contributes an empty list rather than failing the run.
    /// </summary>
    private static IReadOnlyList<string> ParseTransports(string? specJson)
    {
        if (string.IsNullOrWhiteSpace(specJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            using var doc = JsonDocument.Parse(specJson);
            if (!doc.RootElement.TryGetProperty("transports", out var transports) || transports.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            return transports.EnumerateArray()
                .Where(t => t.ValueKind == JsonValueKind.String)
                .Select(t => t.GetString()!)
                .ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    // A cycle-guard alone bounds recursion by ref name, but an inline (ref-less) self-referential
    // shape has no name to guard on - this hard depth cap is the backstop that keeps a pathological
    // spec from stalling a run. Deep enough that no realistic payload is truncated.
    private const int MaxSchemaDepth = 32;

    /// <summary>
    /// Reads a service spec's <c>components.schemas</c> map (name → schema element) for <c>$ref</c>
    /// resolution. Empty when the spec has no components block.
    /// </summary>
    private static Dictionary<string, JsonElement> ReadComponents(JsonElement root)
    {
        var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (root.TryGetProperty("components", out var components) && components.ValueKind == JsonValueKind.Object
            && components.TryGetProperty("schemas", out var schemas) && schemas.ValueKind == JsonValueKind.Object)
        {
            foreach (var schema in schemas.EnumerateObject())
            {
                map[schema.Name] = schema.Value;
            }
        }

        return map;
    }

    /// <summary>
    /// Pulls the named child schema (<c>request</c>/<c>response</c>/<c>message</c>) off a spec
    /// <c>requests</c>/<c>events</c> entry and returns it fully self-contained (all <c>$ref</c>s into
    /// <paramref name="components"/> inlined), or <c>null</c> when the entry carries no such schema.
    /// The returned nodes are detached from the source <see cref="JsonDocument"/>, so they stay valid
    /// after it is disposed.
    /// </summary>
    private static JsonObject? ExtractSchema(JsonElement entry, string propertyName, IReadOnlyDictionary<string, JsonElement> components)
    {
        if (!entry.TryGetProperty(propertyName, out var node) || node.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return InlineSchema(node, components, new HashSet<string>(StringComparer.Ordinal), 0) as JsonObject;
    }

    /// <summary>
    /// Recursively inlines a schema element into a detached <see cref="JsonNode"/>: replaces each
    /// <c>$ref</c> with the referenced component (tagging it with a <c>title</c> of the ref name),
    /// recurses through <c>properties</c>/<c>items</c>/<c>additionalProperties</c>, and copies every
    /// other key (<c>type</c>/<c>required</c>/<c>enum</c>/<c>format</c>/<c>minimum</c>/<c>pattern</c>/…)
    /// verbatim - so downstream (comparison + the UI renderer) never has to resolve a ref. Recursive
    /// types are cut with a <c>title</c>-only marker.
    /// </summary>
    private static JsonNode? InlineSchema(JsonElement node, IReadOnlyDictionary<string, JsonElement> components, HashSet<string> visiting, int depth)
    {
        if (depth > MaxSchemaDepth)
        {
            return new JsonObject { ["type"] = "object" };
        }

        if (node.ValueKind != JsonValueKind.Object)
        {
            return CloneValue(node);
        }

        if (node.TryGetProperty("$ref", out var refElement) && refElement.ValueKind == JsonValueKind.String)
        {
            var name = RefName(refElement.GetString());
            if (name == null || !components.TryGetValue(name, out var target))
            {
                return new JsonObject();
            }

            if (!visiting.Add(name))
            {
                return new JsonObject { ["type"] = "object", ["title"] = name + " (recursive)" };
            }

            var resolved = InlineSchema(target, components, visiting, depth + 1);
            visiting.Remove(name);
            if (resolved is JsonObject resolvedObject && resolvedObject["title"] == null)
            {
                resolvedObject["title"] = name;
            }

            return resolved;
        }

        var result = new JsonObject();
        foreach (var property in node.EnumerateObject())
        {
            switch (property.Name)
            {
                case "properties" when property.Value.ValueKind == JsonValueKind.Object:
                    var properties = new JsonObject();
                    foreach (var member in property.Value.EnumerateObject())
                    {
                        properties[member.Name] = InlineSchema(member.Value, components, visiting, depth + 1);
                    }
                    result["properties"] = properties;
                    break;
                case "items":
                    result["items"] = InlineSchema(property.Value, components, visiting, depth + 1);
                    break;
                case "additionalProperties" when property.Value.ValueKind == JsonValueKind.Object:
                    result["additionalProperties"] = InlineSchema(property.Value, components, visiting, depth + 1);
                    break;
                default:
                    result[property.Name] = CloneValue(property.Value);
                    break;
            }
        }

        return result;
    }

    private static JsonNode? CloneValue(JsonElement element) => JsonNode.Parse(element.GetRawText());

    private static string? RefName(string? reference) =>
        string.IsNullOrEmpty(reference) ? null : reference!.Substring(reference.LastIndexOf('/') + 1);

    /// <summary>
    /// A stable, key-order-independent serialization of a schema node, used only to compare two
    /// consumers' payloads for equality (object keys sorted; arrays kept in order since JSON Schema
    /// arrays like <c>required</c>/<c>enum</c> are order-significant to a producer but two specs
    /// generated the same way emit them the same way). <c>null</c> renders as the literal
    /// <c>"null"</c>, distinct from an empty object.
    /// </summary>
    private static string Canonical(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return "null";
            case JsonObject obj:
                var builder = new StringBuilder("{");
                var first = true;
                foreach (var member in obj.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }
                    first = false;
                    builder.Append(JsonSerializer.Serialize(member.Key)).Append(':').Append(Canonical(member.Value));
                }
                return builder.Append('}').ToString();
            case JsonArray array:
                return "[" + string.Join(",", array.Select(Canonical)) + "]";
            default:
                return node.ToJsonString();
        }
    }

    private IMeshServiceSource ResolveSource(string sourceKey)
    {
        return _sources.TryGetValue(sourceKey, out var source) ? source : new UnknownMeshServiceSource(sourceKey);
    }

    /// <summary>
    /// A service's <see cref="MeshServiceSnapshot.Health"/> being unreachable/undeserializable is
    /// treated as <see cref="MeshServiceStatus.Unreachable"/> regardless of whether its spec endpoint
    /// happened to respond - health is the primary "is this service okay" signal, so not having one
    /// at all is the more important fact to surface than a spec fetch succeeding in isolation.
    /// </summary>
    private static string DetermineStatus(MeshServiceSnapshot snapshot)
    {
        if (snapshot.Health == null)
        {
            return MeshServiceStatus.Unreachable;
        }

        return snapshot.Health.IsHealthy ? MeshServiceStatus.Healthy : MeshServiceStatus.Unhealthy;
    }
}
