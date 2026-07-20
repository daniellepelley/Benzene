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
