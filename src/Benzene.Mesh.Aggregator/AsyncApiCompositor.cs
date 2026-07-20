using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Merges each service's own AsyncAPI 2.0 document (fetched from its <c>spec</c> endpoint with
/// <c>type=asyncapi</c>) into one fleet-wide AsyncAPI 2.0 document loadable in an AsyncAPI editor.
/// </summary>
/// <remarks>
/// Purely JSON-level (`System.Text.Json`, no AsyncAPI object-model dependency, keeping this package
/// dependency-light). Benzene's per-service AsyncAPI keys channels by raw topic id and component
/// schemas by bare CLR type name — both collide across services — so each service's content is
/// <em>namespaced</em>: channel keys become <c>&lt;service&gt;/&lt;topic&gt;</c>, schema keys
/// <c>&lt;Service&gt;_&lt;Type&gt;</c>, and every <c>$ref</c> into <c>components.schemas</c> is
/// rewritten to match before union, so nothing overwrites. Reserved (utility) topics are dropped,
/// and each channel's operations are tagged with the owning service so the editor groups them.
/// Two services that share a topic id stay as two distinct, attributed channels rather than being
/// forced into one.
/// </remarks>
public static class AsyncApiCompositor
{
    /// <summary>One service's fetched AsyncAPI doc plus what the merge needs to namespace/filter it.</summary>
    /// <param name="ServiceName">The service name — the namespace discriminator (channels/schemas/tags).</param>
    /// <param name="AsyncApiJson">The service's raw AsyncAPI 2.0 JSON.</param>
    /// <param name="ReservedTopics">The service's reserved (utility) topic ids, whose channels are skipped.</param>
    public readonly record struct ServiceDocument(string ServiceName, string AsyncApiJson, IReadOnlySet<string> ReservedTopics);

    private const string SchemaRefPrefix = "#/components/schemas/";
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>
    /// Merges the given services' AsyncAPI docs into one composite AsyncAPI 2.0 JSON document. A
    /// service whose doc is missing/unparseable is skipped (best-effort); an empty input still
    /// produces a valid empty-channels document, so a published <c>asyncapi.json</c> is always
    /// loadable.
    /// </summary>
    public static string Merge(IReadOnlyList<ServiceDocument> services, DateTimeOffset generatedAt)
    {
        var channels = new JsonObject();
        var schemas = new JsonObject();
        var tags = new JsonArray();
        var merged = 0;

        foreach (var service in services)
        {
            var doc = TryParse(service.AsyncApiJson);
            if (doc == null)
            {
                continue;
            }

            merged++;
            tags.Add(new JsonObject { ["name"] = service.ServiceName });
            var ns = Slug(service.ServiceName);

            // Rename map for this service's component schemas (old name -> namespaced name).
            var renames = new Dictionary<string, string>(StringComparer.Ordinal);
            if (doc["components"] is JsonObject components && components["schemas"] is JsonObject serviceSchemas)
            {
                foreach (var schema in serviceSchemas)
                {
                    renames[schema.Key] = SchemaKey(service.ServiceName, schema.Key);
                }
            }

            // Rewrite every $ref to components.schemas across the whole doc BEFORE moving anything,
            // so nested inter-schema refs and channel payload refs all point at the namespaced keys.
            RewriteSchemaRefs(doc, renames);

            if (doc["components"] is JsonObject components2 && components2["schemas"] is JsonObject serviceSchemas2)
            {
                foreach (var schema in serviceSchemas2.ToList())
                {
                    schemas[renames[schema.Key]] = schema.Value?.DeepClone();
                }
            }

            if (doc["channels"] is JsonObject serviceChannels)
            {
                foreach (var channel in serviceChannels.ToList())
                {
                    if (service.ReservedTopics.Contains(BaseTopic(channel.Key)))
                    {
                        continue; // drop utility topics (spec/health/mesh) from the event view
                    }

                    if (channel.Value?.DeepClone() is not JsonObject cloned)
                    {
                        continue;
                    }

                    TagOperations(cloned, service.ServiceName);
                    channels[$"{ns}/{channel.Key}"] = cloned;
                }
            }
        }

        var document = new JsonObject
        {
            ["asyncapi"] = "2.0.0",
            ["info"] = new JsonObject
            {
                ["title"] = "Benzene Mesh — Composite AsyncAPI",
                ["version"] = generatedAt.ToString("O"),
                ["description"] = $"Composite of {merged} service(s)' AsyncAPI specs across the mesh, generated by Benzene Mesh."
            },
            ["tags"] = tags,
            ["channels"] = channels,
            ["components"] = new JsonObject { ["schemas"] = schemas }
        };

        return document.ToJsonString(WriteOptions);
    }

    private static JsonObject? TryParse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void RewriteSchemaRefs(JsonNode? node, IReadOnlyDictionary<string, string> renames)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj["$ref"] is JsonValue refValue && refValue.TryGetValue<string>(out var reference)
                    && reference.StartsWith(SchemaRefPrefix, StringComparison.Ordinal))
                {
                    var name = reference.Substring(SchemaRefPrefix.Length);
                    if (renames.TryGetValue(name, out var renamed))
                    {
                        obj["$ref"] = SchemaRefPrefix + renamed;
                    }
                }

                foreach (var property in obj.ToList())
                {
                    if (property.Key != "$ref")
                    {
                        RewriteSchemaRefs(property.Value, renames);
                    }
                }
                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    RewriteSchemaRefs(item, renames);
                }
                break;
        }
    }

    private static void TagOperations(JsonObject channel, string serviceName)
    {
        foreach (var operationName in new[] { "publish", "subscribe" })
        {
            if (channel[operationName] is not JsonObject operation)
            {
                continue;
            }

            if (operation["tags"] is not JsonArray tags)
            {
                tags = new JsonArray();
                operation["tags"] = tags;
            }

            tags.Add(new JsonObject { ["name"] = serviceName });
        }
    }

    // Benzene emits a handled topic's response on a "<topic>:benzeneResult" channel - strip that
    // suffix so the reserved-topic check matches the base topic id.
    private static string BaseTopic(string channelKey)
    {
        const string resultSuffix = ":benzeneResult";
        return channelKey.EndsWith(resultSuffix, StringComparison.Ordinal)
            ? channelKey.Substring(0, channelKey.Length - resultSuffix.Length)
            : channelKey;
    }

    private static string SchemaKey(string serviceName, string typeName)
    {
        var prefix = string.Concat(Slug(serviceName)
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1)));
        return prefix.Length == 0 ? typeName : prefix + "_" + typeName;
    }

    private static string Slug(string value)
    {
        var lowered = new string((value ?? string.Empty).Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (lowered.Contains("--"))
        {
            lowered = lowered.Replace("--", "-");
        }

        lowered = lowered.Trim('-');
        return lowered.Length == 0 ? "service" : lowered;
    }
}
