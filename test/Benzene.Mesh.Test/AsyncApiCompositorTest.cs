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

    // A minimal but real-shaped per-service AsyncAPI 2.0 doc: one handled topic whose payload $refs a
    // component schema, matching Benzene's AsyncApiDocumentBuilder output shape.
    private static string ServiceDoc(string title, string topic, string schemaName) => $$"""
    {
      "asyncapi": "2.0.0",
      "info": { "title": "{{title}}", "version": "1.0" },
      "channels": {
        "{{topic}}": { "subscribe": { "operationId": "{{topic}}", "message": { "name": "{{topic}}", "payload": { "$ref": "#/components/schemas/{{schemaName}}" } } } }
      },
      "components": { "schemas": { "{{schemaName}}": { "type": "object", "properties": { "id": { "type": "string" } } } } }
    }
    """;

    [Fact]
    public void Merge_NamespacesChannelsAndSchemas_AndRewritesRefs()
    {
        // Two services that BOTH handle order:create and BOTH define a "Customer" schema - the exact
        // collision case a naive union would silently overwrite.
        var merged = AsyncApiCompositor.Merge(new[]
        {
            Service("orders-api", ServiceDoc("orders-api", "order:create", "Customer")),
            Service("fulfilment-api", ServiceDoc("fulfilment-api", "order:create", "Customer")),
        }, At);

        var doc = JsonNode.Parse(merged)!.AsObject();

        Assert.Equal("2.0.0", doc["asyncapi"]!.GetValue<string>());

        // Both services' channels survive, namespaced by service - no collision.
        var channels = doc["channels"]!.AsObject();
        Assert.True(channels.ContainsKey("orders-api/order:create"));
        Assert.True(channels.ContainsKey("fulfilment-api/order:create"));
        Assert.False(channels.ContainsKey("order:create"));

        // Both "Customer" schemas survive, namespaced.
        var schemas = doc["components"]!["schemas"]!.AsObject();
        Assert.True(schemas.ContainsKey("OrdersApi_Customer"));
        Assert.True(schemas.ContainsKey("FulfilmentApi_Customer"));

        // The channel payload $ref was rewritten to the namespaced schema key.
        var payloadRef = channels["orders-api/order:create"]!["subscribe"]!["message"]!["payload"]!["$ref"]!.GetValue<string>();
        Assert.Equal("#/components/schemas/OrdersApi_Customer", payloadRef);

        // Each channel's operation is tagged with its owning service.
        var tag = channels["fulfilment-api/order:create"]!["subscribe"]!["tags"]!.AsArray()[0]!["name"]!.GetValue<string>();
        Assert.Equal("fulfilment-api", tag);

        // AsyncAPI 2.0 requires a document-globally-unique operationId. Both services key theirs by
        // the same topic ("order:create"), so the merge must namespace them apart, not collide.
        var ordersOpId = channels["orders-api/order:create"]!["subscribe"]!["operationId"]!.GetValue<string>();
        var fulfilmentOpId = channels["fulfilment-api/order:create"]!["subscribe"]!["operationId"]!.GetValue<string>();
        Assert.Equal("orders-api_order:create", ordersOpId);
        Assert.Equal("fulfilment-api_order:create", fulfilmentOpId);
        Assert.NotEqual(ordersOpId, fulfilmentOpId);
    }

    [Fact]
    public void Merge_AllOperationIdsAreUniqueAcrossTheDocument()
    {
        // Two services sharing a topic id AND a second pair sharing another — every emitted
        // operationId must still be distinct across the whole composite document.
        var merged = AsyncApiCompositor.Merge(new[]
        {
            Service("orders-api", ServiceDoc("orders-api", "order:create", "Order")),
            Service("fulfilment-api", ServiceDoc("fulfilment-api", "order:create", "Order")),
            Service("billing-api", ServiceDoc("billing-api", "order:create", "Order")),
        }, At);

        var channels = JsonNode.Parse(merged)!["channels"]!.AsObject();
        var operationIds = channels
            .Select(c => c.Value!["subscribe"]?["operationId"]?.GetValue<string>())
            .Where(id => id != null)
            .ToList();

        Assert.Equal(3, operationIds.Count);
        Assert.Equal(operationIds.Count, operationIds.Distinct().Count());
    }

    [Fact]
    public void Merge_PrunesSchemasOnlyReferencedByDroppedReservedChannels()
    {
        // The reserved "spec" channel references SpecRequest; the domain "order:create" channel
        // references Order (which nests OrderChild). Dropping the reserved channel must also drop
        // SpecRequest (now orphaned), but keep Order and its nested OrderChild.
        var doc = """
        {
          "asyncapi": "2.0.0",
          "info": { "title": "orders-api", "version": "1.0" },
          "channels": {
            "order:create": { "subscribe": { "operationId": "order:create", "message": { "payload": { "$ref": "#/components/schemas/Order" } } } },
            "spec": { "subscribe": { "operationId": "spec", "message": { "payload": { "$ref": "#/components/schemas/SpecRequest" } } } }
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
    public void Merge_EmitsDefaultContentTypeAndId()
    {
        var merged = AsyncApiCompositor.Merge(new[]
        {
            Service("orders-api", ServiceDoc("orders-api", "order:create", "Order")),
        }, At);

        var doc = JsonNode.Parse(merged)!.AsObject();
        Assert.Equal("application/json", doc["defaultContentType"]!.GetValue<string>());
        Assert.False(string.IsNullOrEmpty(doc["id"]!.GetValue<string>()));
    }

    [Fact]
    public void Merge_SkipsReservedTopicChannels()
    {
        var doc = """
        {
          "asyncapi": "2.0.0",
          "info": { "title": "orders-api", "version": "1.0" },
          "channels": {
            "order:create": { "subscribe": { "operationId": "order:create" } },
            "spec": { "subscribe": { "operationId": "spec" } },
            "spec:benzeneResult": { "publish": { "operationId": "spec:benzeneResult" } }
          },
          "components": { "schemas": {} }
        }
        """;

        var merged = AsyncApiCompositor.Merge(new[] { Service("orders-api", doc, "spec") }, At);
        var channels = JsonNode.Parse(merged)!["channels"]!.AsObject();

        Assert.True(channels.ContainsKey("orders-api/order:create"));
        // The reserved "spec" topic and its :benzeneResult response channel are both dropped.
        Assert.DoesNotContain(channels, kv => kv.Key.Contains("spec"));
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
        Assert.True(channels.ContainsKey("good/order:create"));
    }

    [Fact]
    public void Merge_NoServices_ProducesValidEmptyDocument()
    {
        var merged = AsyncApiCompositor.Merge(Array.Empty<AsyncApiCompositor.ServiceDocument>(), At);
        var doc = JsonNode.Parse(merged)!.AsObject();

        Assert.Equal("2.0.0", doc["asyncapi"]!.GetValue<string>());
        Assert.Empty(doc["channels"]!.AsObject());
    }

    [Fact]
    public void Merge_ProducesWellFormedAsyncApi20_WithExpectedShape()
    {
        // Structural validity of the emitted shape. (That the document actually LOADS in the real
        // AsyncAPI reader / editor is proven separately with AsyncAPI.NET.Readers, in an isolated
        // project - the Mesh test project can't reference that reader because its transitive
        // KubernetesClient pulls an incompatible YamlDotNet.)
        var merged = AsyncApiCompositor.Merge(new[]
        {
            Service("orders-api", ServiceDoc("orders-api", "order:create", "Customer")),
            Service("fulfilment-api", ServiceDoc("fulfilment-api", "order:submitted", "Shipment")),
        }, At);

        var doc = JsonNode.Parse(merged)!.AsObject();
        Assert.Equal("2.0.0", doc["asyncapi"]!.GetValue<string>());
        Assert.NotNull(doc["info"]!["title"]);
        Assert.Equal(2, doc["channels"]!.AsObject().Count);
        Assert.Equal(2, doc["components"]!["schemas"]!.AsObject().Count);
        // Every channel payload $ref resolves to a schema that actually exists in components.
        var schemaKeys = doc["components"]!["schemas"]!.AsObject().Select(kv => kv.Key).ToHashSet();
        foreach (var channel in doc["channels"]!.AsObject())
        {
            var payloadRef = channel.Value!["subscribe"]?["message"]?["payload"]?["$ref"]?.GetValue<string>();
            if (payloadRef != null)
            {
                Assert.Contains(payloadRef.Substring("#/components/schemas/".Length), schemaKeys);
            }
        }
    }
}
