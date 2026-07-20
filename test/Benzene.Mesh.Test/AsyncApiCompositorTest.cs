using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Benzene.Mesh.Aggregator;
using Xunit;

namespace Benzene.Mesh.Test;

public class AsyncApiCompositorTest
{
    private static readonly DateTimeOffset At = DateTimeOffset.UnixEpoch;

    private static AsyncApiCompositor.ServiceDocument Service(string name, string json, params string[] reserved) =>
        new(name, json, new HashSet<string>(reserved, StringComparer.Ordinal));

    // AsyncAPI 3.0 channel/operation map keys must be identifiers; the raw topic lives in `address`.
    private static string ChKey(string topic) => topic.Replace(":", "_");

    // A minimal but real-shaped per-service AsyncAPI 3.0 doc: one handled topic (channel + receive
    // operation) whose message payload $refs a component schema, matching AsyncApiDocumentBuilder output.
    private static string ServiceDoc(string title, string topic, string schemaName)
    {
        var ch = ChKey(topic);
        return $$"""
        {
          "asyncapi": "3.0.0",
          "info": { "title": "{{title}}", "version": "1.0" },
          "channels": {
            "{{ch}}": { "address": "{{topic}}", "messages": { "{{schemaName}}": { "payload": { "$ref": "#/components/schemas/{{schemaName}}" } } } }
          },
          "operations": {
            "{{ch}}": { "action": "receive", "channel": { "$ref": "#/channels/{{ch}}" }, "messages": [ { "$ref": "#/channels/{{ch}}/messages/{{schemaName}}" } ] }
          },
          "components": { "schemas": { "{{schemaName}}": { "type": "object", "properties": { "id": { "type": "string" } } } } }
        }
        """;
    }

    [Fact]
    public void Merge_NamespacesChannelsOperationsAndSchemas_AndRewritesRefs()
    {
        // Two services that BOTH handle order:create and BOTH define a "Customer" schema - the exact
        // collision case a naive union would silently overwrite.
        var merged = AsyncApiCompositor.Merge(new[]
        {
            Service("orders-api", ServiceDoc("orders-api", "order:create", "Customer")),
            Service("fulfilment-api", ServiceDoc("fulfilment-api", "order:create", "Customer")),
        }, At);

        var doc = JsonNode.Parse(merged)!.AsObject();
        Assert.Equal("3.0.0", doc["asyncapi"]!.GetValue<string>());

        // Both services' channels + operations survive, namespaced by service - no collision.
        var channels = doc["channels"]!.AsObject();
        Assert.True(channels.ContainsKey("orders-api_order_create"));
        Assert.True(channels.ContainsKey("fulfilment-api_order_create"));
        Assert.False(channels.ContainsKey("order_create"));

        var operations = doc["operations"]!.AsObject();
        Assert.True(operations.ContainsKey("orders-api_order_create"));
        Assert.True(operations.ContainsKey("fulfilment-api_order_create"));

        // Both "Customer" schemas survive, namespaced.
        var schemas = doc["components"]!["schemas"]!.AsObject();
        Assert.True(schemas.ContainsKey("OrdersApi_Customer"));
        Assert.True(schemas.ContainsKey("FulfilmentApi_Customer"));

        // The message payload $ref was rewritten to the namespaced schema key.
        var payloadRef = channels["orders-api_order_create"]!["messages"]!["Customer"]!["payload"]!["$ref"]!.GetValue<string>();
        Assert.Equal("#/components/schemas/OrdersApi_Customer", payloadRef);

        // The operation's channel and message $refs were rewritten onto the namespaced channel key.
        var op = operations["orders-api_order_create"]!;
        Assert.Equal("#/channels/orders-api_order_create", op["channel"]!["$ref"]!.GetValue<string>());
        Assert.Equal("#/channels/orders-api_order_create/messages/Customer", op["messages"]!.AsArray()[0]!["$ref"]!.GetValue<string>());

        // The address (the real topic) is preserved on the channel.
        Assert.Equal("order:create", channels["orders-api_order_create"]!["address"]!.GetValue<string>());

        // Each operation is tagged with its owning service.
        Assert.Equal("fulfilment-api", operations["fulfilment-api_order_create"]!["tags"]!.AsArray()[0]!["name"]!.GetValue<string>());
    }

    [Fact]
    public void Merge_AllOperationKeysAreUniqueAcrossTheDocument()
    {
        // Three services sharing a topic id — every emitted operation key must still be distinct.
        var merged = AsyncApiCompositor.Merge(new[]
        {
            Service("orders-api", ServiceDoc("orders-api", "order:create", "Order")),
            Service("fulfilment-api", ServiceDoc("fulfilment-api", "order:create", "Order")),
            Service("billing-api", ServiceDoc("billing-api", "order:create", "Order")),
        }, At);

        var operations = JsonNode.Parse(merged)!["operations"]!.AsObject();
        Assert.Equal(3, operations.Count);
        Assert.Equal(operations.Count, operations.Select(o => o.Key).Distinct().Count());
    }

    [Fact]
    public void Merge_PrunesSchemasOnlyReferencedByDroppedReservedChannels()
    {
        // The reserved "spec" channel references SpecRequest; the domain "order:create" channel
        // references Order (which nests OrderChild). Dropping the reserved channel must also drop
        // SpecRequest (now orphaned), but keep Order and its nested OrderChild.
        var doc = """
        {
          "asyncapi": "3.0.0",
          "info": { "title": "orders-api", "version": "1.0" },
          "channels": {
            "order_create": { "address": "order:create", "messages": { "Order": { "payload": { "$ref": "#/components/schemas/Order" } } } },
            "spec": { "address": "spec", "messages": { "SpecRequest": { "payload": { "$ref": "#/components/schemas/SpecRequest" } } } }
          },
          "operations": {
            "order_create": { "action": "receive", "channel": { "$ref": "#/channels/order_create" }, "messages": [ { "$ref": "#/channels/order_create/messages/Order" } ] },
            "spec": { "action": "receive", "channel": { "$ref": "#/channels/spec" }, "messages": [ { "$ref": "#/channels/spec/messages/SpecRequest" } ] }
          },
          "components": { "schemas": {
            "Order": { "type": "object", "properties": { "child": { "$ref": "#/components/schemas/OrderChild" } } },
            "OrderChild": { "type": "object", "properties": { "n": { "type": "integer" } } },
            "SpecRequest": { "type": "object", "properties": { "type": { "type": "string" } } }
          } }
        }
        """;

        var merged = AsyncApiCompositor.Merge(new[] { Service("orders-api", doc, "spec") }, At);
        var schemas = JsonNode.Parse(merged)!["components"]!["schemas"]!.AsObject();

        Assert.True(schemas.ContainsKey("OrdersApi_Order"));
        Assert.True(schemas.ContainsKey("OrdersApi_OrderChild")); // reachable via Order's nested $ref
        Assert.False(schemas.ContainsKey("OrdersApi_SpecRequest")); // orphaned by the dropped reserved channel
    }

    [Fact]
    public void Merge_EmitsAsyncApi30WithDefaultContentTypeAndId()
    {
        var merged = AsyncApiCompositor.Merge(new[]
        {
            Service("orders-api", ServiceDoc("orders-api", "order:create", "Order")),
        }, At);

        var doc = JsonNode.Parse(merged)!.AsObject();
        Assert.Equal("3.0.0", doc["asyncapi"]!.GetValue<string>());
        Assert.Equal("application/json", doc["defaultContentType"]!.GetValue<string>());
        Assert.False(string.IsNullOrEmpty(doc["id"]!.GetValue<string>()));
    }

    [Fact]
    public void Merge_SkipsReservedTopicChannelsAndOperations()
    {
        var doc = """
        {
          "asyncapi": "3.0.0",
          "info": { "title": "orders-api", "version": "1.0" },
          "channels": {
            "order_create": { "address": "order:create", "messages": {} },
            "spec": { "address": "spec", "messages": {} },
            "spec_benzeneResult": { "address": "spec:benzeneResult", "messages": {} }
          },
          "operations": {
            "order_create": { "action": "receive", "channel": { "$ref": "#/channels/order_create" } },
            "spec": { "action": "receive", "channel": { "$ref": "#/channels/spec" } }
          },
          "components": { "schemas": {} }
        }
        """;

        var merged = AsyncApiCompositor.Merge(new[] { Service("orders-api", doc, "spec") }, At);
        var parsed = JsonNode.Parse(merged)!;
        var channels = parsed["channels"]!.AsObject();
        var operations = parsed["operations"]!.AsObject();

        Assert.True(channels.ContainsKey("orders-api_order_create"));
        // The reserved "spec" topic, its :benzeneResult reply channel, and its operation are all dropped.
        Assert.DoesNotContain(channels, kv => kv.Key.Contains("spec"));
        Assert.DoesNotContain(operations, kv => kv.Key.Contains("spec"));
    }

    [Fact]
    public void Merge_UnparseableOrEmptyService_IsSkipped_NotFatal()
    {
        var merged = AsyncApiCompositor.Merge(new[]
        {
            Service("broken", "}{ not json"),
            Service("empty", ""),
            Service("good", ServiceDoc("good", "order:create", "Order")),
        }, At);

        var channels = JsonNode.Parse(merged)!["channels"]!.AsObject();
        Assert.Single(channels);
        Assert.True(channels.ContainsKey("good_order_create"));
    }

    [Fact]
    public void Merge_NoServices_ProducesValidEmptyDocument()
    {
        var merged = AsyncApiCompositor.Merge(Array.Empty<AsyncApiCompositor.ServiceDocument>(), At);
        var doc = JsonNode.Parse(merged)!.AsObject();

        Assert.Equal("3.0.0", doc["asyncapi"]!.GetValue<string>());
        Assert.Empty(doc["channels"]!.AsObject());
        Assert.Empty(doc["operations"]!.AsObject());
    }

    [Fact]
    public void Merge_ProducesWellFormedAsyncApi30_WithExpectedShape()
    {
        // Structural validity of the emitted shape. (That the document actually LOADS in the real
        // AsyncAPI 3.0 reader is proven separately with ByteBard.AsyncAPI.NET.Readers, in an isolated
        // project - the Mesh test project can't reference that reader because its transitive
        // KubernetesClient pulls an incompatible YamlDotNet.)
        var merged = AsyncApiCompositor.Merge(new[]
        {
            Service("orders-api", ServiceDoc("orders-api", "order:create", "Customer")),
            Service("fulfilment-api", ServiceDoc("fulfilment-api", "order:submitted", "Shipment")),
        }, At);

        var doc = JsonNode.Parse(merged)!.AsObject();
        Assert.Equal("3.0.0", doc["asyncapi"]!.GetValue<string>());
        Assert.NotNull(doc["info"]!["title"]);
        Assert.Equal(2, doc["channels"]!.AsObject().Count);
        Assert.Equal(2, doc["operations"]!.AsObject().Count);
        Assert.Equal(2, doc["components"]!["schemas"]!.AsObject().Count);

        // Every message payload $ref resolves to a schema that actually exists in components.
        var schemaKeys = doc["components"]!["schemas"]!.AsObject().Select(kv => kv.Key).ToHashSet();
        foreach (var channel in doc["channels"]!.AsObject())
        {
            foreach (var message in channel.Value!["messages"]!.AsObject())
            {
                var payloadRef = message.Value!["payload"]?["$ref"]?.GetValue<string>();
                if (payloadRef != null)
                {
                    Assert.Contains(payloadRef.Substring("#/components/schemas/".Length), schemaKeys);
                }
            }
        }

        // Every operation's channel $ref resolves to a channel that actually exists.
        var channelKeys = doc["channels"]!.AsObject().Select(kv => kv.Key).ToHashSet();
        foreach (var operation in doc["operations"]!.AsObject())
        {
            var channelRef = operation.Value!["channel"]!["$ref"]!.GetValue<string>();
            Assert.Contains(channelRef.Substring("#/channels/".Length), channelKeys);
        }
    }
}
