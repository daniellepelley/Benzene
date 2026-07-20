using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Merges each service's own AsyncAPI 3.0 document (fetched from its <c>spec</c> endpoint with
/// <c>type=asyncapi</c>) into one fleet-wide AsyncAPI 3.0 document loadable in an AsyncAPI editor.
/// </summary>
/// <remarks>
/// Purely JSON-level (<c>System.Text.Json</c>, no AsyncAPI object-model dependency, keeping this
/// package dependency-light). AsyncAPI 3.0 keys <c>channels</c> and top-level <c>operations</c> by
/// identifier and component schemas by bare CLR type name — all collide across services — so each
/// service's content is <em>namespaced</em>: channel keys become <c>&lt;service&gt;_&lt;channel&gt;</c>,
/// operation keys <c>&lt;service&gt;_&lt;operation&gt;</c>, schema keys <c>&lt;Service&gt;_&lt;Type&gt;</c>,
/// and every <c>$ref</c> (into <c>components.schemas</c>, into <c>#/channels/…</c>, and into a channel's
/// <c>…/messages/…</c>) is rewritten to match before union, so nothing overwrites. Reserved (utility)
/// topics — and the operations referencing them — are dropped, each operation is tagged with the owning
/// service, and component schemas left unreferenced after the merge are pruned. Two services that share a
/// topic stay as two distinct, attributed channels+operations rather than being forced into one.
/// </remarks>
public static class AsyncApiCompositor
{
    /// <summary>One service's fetched AsyncAPI doc plus what the merge needs to namespace/filter it.</summary>
    /// <param name="ServiceName">The service name — the namespace discriminator (channels/operations/schemas/tags).</param>
    /// <param name="AsyncApiJson">The service's raw AsyncAPI 3.0 JSON.</param>
    /// <param name="ReservedTopics">The service's reserved (utility) topic ids, whose channels/operations are skipped.</param>
    public readonly record struct ServiceDocument(string ServiceName, string AsyncApiJson, IReadOnlySet<string> ReservedTopics);

    private const string SchemaRefPrefix = "#/components/schemas/";
    private const string ChannelRefPrefix = "#/channels/";
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    /// <summary>
    /// Merges the given services' AsyncAPI 3.0 docs into one composite AsyncAPI 3.0 JSON document. A
    /// service whose doc is missing/unparseable is skipped (best-effort); an empty input still produces
    /// a valid empty document, so a published <c>asyncapi.json</c> is always loadable.
    /// </summary>
    public static string Merge(IReadOnlyList<ServiceDocument> services, DateTimeOffset generatedAt)
    {
        var channels = new JsonObject();
        var operations = new JsonObject();
        var schemas = new JsonObject();
        var tags = new JsonArray();
        var usedOperationKeys = new HashSet<string>(StringComparer.Ordinal);
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

            // 1) Namespace + move component schemas (rewriting every schema $ref across the doc first,
            //    so channel-message payload refs and nested inter-schema refs all point at the new keys).
            var schemaRenames = new Dictionary<string, string>(StringComparer.Ordinal);
            if (doc["components"] is JsonObject components && components["schemas"] is JsonObject serviceSchemas)
            {
                foreach (var schema in serviceSchemas)
                {
                    schemaRenames[schema.Key] = SchemaKey(service.ServiceName, schema.Key);
                }
            }

            RewriteSchemaRefs(doc, schemaRenames);

            if (doc["components"] is JsonObject components2 && components2["schemas"] is JsonObject serviceSchemas2)
            {
                foreach (var schema in serviceSchemas2.ToList())
                {
                    schemas[schemaRenames[schema.Key]] = schema.Value?.DeepClone();
                }
            }

            // 2) Namespace channels (dropping reserved ones), tracking old->new channel keys so the
            //    operations that reference them can be rewritten.
            var channelKeyMap = new Dictionary<string, string>(StringComparer.Ordinal);
            if (doc["channels"] is JsonObject serviceChannels)
            {
                foreach (var channel in serviceChannels.ToList())
                {
                    if (channel.Value is not JsonObject channelObj)
                    {
                        continue;
                    }

                    var address = channelObj["address"]?.GetValue<string>() ?? channel.Key;
                    if (service.ReservedTopics.Contains(BaseTopic(address)))
                    {
                        continue; // drop utility topics (spec/health/mesh) and their reply channels
                    }

                    var newKey = $"{ns}_{channel.Key}";
                    channelKeyMap[channel.Key] = newKey;
                    channels[newKey] = channelObj.DeepClone();
                }
            }

            // 3) Namespace operations, dropping any that target a dropped (reserved) channel, and
            //    rewriting their channel/message $refs onto the namespaced channel keys.
            if (doc["operations"] is JsonObject serviceOperations)
            {
                foreach (var operation in serviceOperations.ToList())
                {
                    if (operation.Value?.DeepClone() is not JsonObject operationObj)
                    {
                        continue;
                    }

                    if (!TryRewriteOperation(operationObj, channelKeyMap))
                    {
                        continue; // operation targets a reserved/dropped channel
                    }

                    TagOperation(operationObj, service.ServiceName);
                    operations[UniqueKey($"{ns}_{operation.Key}", usedOperationKeys)] = operationObj;
                }
            }
        }

        // Reserved-channel drops can orphan the schemas only those channels' messages referenced; drop
        // any component schema not reachable from a retained channel (validators flag unused components).
        PruneUnusedSchemas(channels, schemas);

        var document = new JsonObject
        {
            ["asyncapi"] = "3.0.0",
            ["id"] = "urn:benzene:mesh:composite",
            ["defaultContentType"] = "application/json",
            ["info"] = new JsonObject
            {
                ["title"] = "Benzene Mesh — Composite AsyncAPI",
                ["version"] = generatedAt.ToString("O"),
                ["description"] = $"Composite of {merged} service(s)' AsyncAPI specs across the mesh, generated by Benzene Mesh.",
                ["tags"] = tags
            },
            ["channels"] = channels,
            ["operations"] = operations,
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

    // Rewrites an operation's channel/message/reply $refs onto the namespaced channel keys. Returns
    // false (drop the operation) if its channel resolves to a dropped (reserved) channel.
    private static bool TryRewriteOperation(JsonObject operation, IReadOnlyDictionary<string, string> channelKeyMap)
    {
        var channelKey = ChannelKeyOf(operation["channel"]);
        if (channelKey == null || !channelKeyMap.ContainsKey(channelKey))
        {
            return false;
        }

        RewriteChannelRef(operation["channel"], channelKeyMap);
        RewriteMessageRefs(operation["messages"], channelKeyMap);

        if (operation["reply"] is JsonObject reply)
        {
            // Only keep the reply if its channel survived; otherwise drop the reply but keep the operation.
            var replyChannelKey = ChannelKeyOf(reply["channel"]);
            if (replyChannelKey != null && channelKeyMap.ContainsKey(replyChannelKey))
            {
                RewriteChannelRef(reply["channel"], channelKeyMap);
                RewriteMessageRefs(reply["messages"], channelKeyMap);
            }
            else
            {
                operation.Remove("reply");
            }
        }

        return true;
    }

    private static string? ChannelKeyOf(JsonNode? channelRefNode)
    {
        if (channelRefNode is JsonObject obj && obj["$ref"] is JsonValue value && value.TryGetValue<string>(out var reference)
            && reference.StartsWith(ChannelRefPrefix, StringComparison.Ordinal))
        {
            var rest = reference.Substring(ChannelRefPrefix.Length);
            var slash = rest.IndexOf('/');
            return slash < 0 ? rest : rest.Substring(0, slash);
        }

        return null;
    }

    private static void RewriteChannelRef(JsonNode? channelRefNode, IReadOnlyDictionary<string, string> channelKeyMap)
    {
        if (channelRefNode is JsonObject obj && obj["$ref"] is JsonValue value && value.TryGetValue<string>(out var reference))
        {
            obj["$ref"] = RewritePointer(reference, channelKeyMap);
        }
    }

    private static void RewriteMessageRefs(JsonNode? messagesNode, IReadOnlyDictionary<string, string> channelKeyMap)
    {
        if (messagesNode is not JsonArray messages)
        {
            return;
        }

        foreach (var message in messages)
        {
            if (message is JsonObject obj && obj["$ref"] is JsonValue value && value.TryGetValue<string>(out var reference))
            {
                obj["$ref"] = RewritePointer(reference, channelKeyMap);
            }
        }
    }

    // Rewrites the channel-id segment of a #/channels/<key>[/messages/<msg>] pointer via the map.
    private static string RewritePointer(string reference, IReadOnlyDictionary<string, string> channelKeyMap)
    {
        if (!reference.StartsWith(ChannelRefPrefix, StringComparison.Ordinal))
        {
            return reference;
        }

        var rest = reference.Substring(ChannelRefPrefix.Length);
        var slash = rest.IndexOf('/');
        var oldKey = slash < 0 ? rest : rest.Substring(0, slash);
        var tail = slash < 0 ? string.Empty : rest.Substring(slash);
        return channelKeyMap.TryGetValue(oldKey, out var newKey) ? ChannelRefPrefix + newKey + tail : reference;
    }

    private static void TagOperation(JsonObject operation, string serviceName)
    {
        if (operation["tags"] is not JsonArray tags)
        {
            tags = new JsonArray();
            operation["tags"] = tags;
        }

        tags.Add(new JsonObject { ["name"] = serviceName });
    }

    // Benzene emits a handled topic's reply on a "<topic>:benzeneResult" channel address - strip that
    // suffix so the reserved-topic check matches the base topic id.
    private static string BaseTopic(string address)
    {
        const string resultSuffix = ":benzeneResult";
        return address.EndsWith(resultSuffix, StringComparison.Ordinal)
            ? address.Substring(0, address.Length - resultSuffix.Length)
            : address;
    }

    private static string UniqueKey(string candidate, ISet<string> used)
    {
        var unique = candidate;
        var suffix = 2;
        while (!used.Add(unique))
        {
            unique = $"{candidate}_{suffix++}";
        }

        return unique;
    }

    // Removes component schemas not reachable from any retained channel (following $refs transitively).
    private static void PruneUnusedSchemas(JsonObject channels, JsonObject schemas)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();

        foreach (var name in FindSchemaRefNames(channels))
        {
            if (reachable.Add(name))
            {
                queue.Enqueue(name);
            }
        }

        while (queue.Count > 0)
        {
            var name = queue.Dequeue();
            if (schemas[name] is not { } schema)
            {
                continue;
            }

            foreach (var nested in FindSchemaRefNames(schema))
            {
                if (reachable.Add(nested))
                {
                    queue.Enqueue(nested);
                }
            }
        }

        foreach (var key in schemas.Select(kv => kv.Key).ToList())
        {
            if (!reachable.Contains(key))
            {
                schemas.Remove(key);
            }
        }
    }

    private static IEnumerable<string> FindSchemaRefNames(JsonNode? node)
    {
        var results = new List<string>();
        Walk(node, results);
        return results;

        static void Walk(JsonNode? n, List<string> into)
        {
            switch (n)
            {
                case JsonObject obj:
                    if (obj["$ref"] is JsonValue value && value.TryGetValue<string>(out var reference)
                        && reference.StartsWith(SchemaRefPrefix, StringComparison.Ordinal))
                    {
                        into.Add(reference.Substring(SchemaRefPrefix.Length));
                    }

                    foreach (var property in obj)
                    {
                        if (property.Key != "$ref")
                        {
                            Walk(property.Value, into);
                        }
                    }
                    break;
                case JsonArray array:
                    foreach (var item in array)
                    {
                        Walk(item, into);
                    }
                    break;
            }
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
