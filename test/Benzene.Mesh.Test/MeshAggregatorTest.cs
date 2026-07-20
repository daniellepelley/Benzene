using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using Benzene.HealthChecks.Core;
using Benzene.Mesh.Aggregator;
using Benzene.Mesh.Contracts;
using Xunit;

namespace Benzene.Mesh.Test;

public class MeshAggregatorTest : IDisposable
{
    private const string SpecUrl = "https://orders-api.example/spec?type=benzene";
    private const string HealthUrl = "https://orders-api.example/healthcheck";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _rootDirectory = Path.Combine(Path.GetTempPath(), "benzene-mesh-aggregator-test-" + Guid.NewGuid());

    private static string SerializeHealth(bool isHealthy)
    {
        var response = new HealthCheckResponse(isHealthy, new Dictionary<string, HealthCheckResult>
        {
            { "Simple", (HealthCheckResult)HealthCheckResult.CreateInstance(isHealthy, "Simple") },
        });
        return JsonSerializer.Serialize(response, JsonOptions);
    }

    private MeshServiceRegistry SingleServiceRegistry() =>
        new(new[] { new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl) });

    [Fact]
    public async Task RunOnceAsync_HealthyService_FirstRun_ReportsHealthyNoDrift()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        var entry = Assert.Single(manifest.Services);
        Assert.Equal(MeshServiceStatus.Healthy, entry.Status);
        Assert.False(entry.ContractDrift);
    }

    [Fact]
    public async Task RunOnceAsync_PublishesTopicsCatalog_AcrossServices_WithReservedFlag()
    {
        const string paymentsSpecUrl = "https://payments-api.example/spec?type=benzene";
        const string paymentsHealthUrl = "https://payments-api.example/healthcheck";
        var ordersSpec = "{\"requests\":[{\"topic\":\"order:create\",\"httpMappings\":[{\"method\":\"post\",\"path\":\"/orders\"}]},{\"topic\":\"spec\",\"reserved\":true}]}";
        var paymentsSpec = "{\"requests\":[{\"topic\":\"payment:take\"},{\"topic\":\"spec\",\"reserved\":true}]}";

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, ordersSpec)
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true))
            .MapGet(paymentsSpecUrl, HttpStatusCode.OK, paymentsSpec)
            .MapGet(paymentsHealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl),
            new MeshServiceRegistryEntry("payments-api", paymentsSpecUrl, paymentsHealthUrl),
        }));

        var json = await store.TryReadAsync("topics.json");
        Assert.NotNull(json);
        var catalog = JsonSerializer.Deserialize<MeshTopicCatalog>(json!, JsonOptions)!;

        // The reserved 'spec' topic is exposed by both services, flagged reserved, and never
        // gets a Status even though it has zero producers - a health/spec endpoint has no
        // "producer" in the fleet sense, so the absence of one isn't informative.
        var spec = Assert.Single(catalog.Topics, t => t.Topic == "spec");
        Assert.True(spec.Reserved);
        Assert.Null(spec.Status);
        Assert.Equal(new[] { "orders-api", "payments-api" }, spec.Consumers.Select(s => s.Service).OrderBy(x => x).ToArray());

        // A domain topic is owned by one service, not reserved, with its HTTP mapping preserved -
        // and even though it has zero producers, it's HTTP-invoked so it's not flagged "gap"
        // (an HTTP endpoint's producer is inherently an external caller, not a fleet declaration).
        var create = Assert.Single(catalog.Topics, t => t.Topic == "order:create");
        Assert.False(create.Reserved);
        Assert.Null(create.Status);
        var svc = Assert.Single(create.Consumers);
        Assert.Equal("orders-api", svc.Service);
        Assert.Equal("post", Assert.Single(svc.HttpMappings).Method);
        Assert.Empty(create.Producers);

        // Domain topics sort ahead of reserved utilities.
        Assert.False(catalog.Topics[0].Reserved);
        Assert.True(catalog.Topics[^1].Reserved);
    }

    [Fact]
    public async Task RunOnceAsync_PublishesCompositeAsyncApi_FromEachServicesAsyncApiEndpoint()
    {
        const string paymentsSpecUrl = "https://payments-api.example/spec?type=benzene";
        const string paymentsHealthUrl = "https://payments-api.example/healthcheck";

        // Each service serves its benzene spec (topics) at type=benzene and its AsyncAPI 3.0 doc at
        // the derived type=asyncapi URL. Both declare a "spec" utility topic that must be filtered.
        var ordersBenzene = """{"requests":[{"topic":"order:create"},{"topic":"spec","reserved":true}]}""";
        var paymentsBenzene = """{"requests":[{"topic":"payment:take"},{"topic":"spec","reserved":true}]}""";
        var ordersAsyncApi = AsyncApiDoc("orders-api", "order:create", "Order");
        var paymentsAsyncApi = AsyncApiDoc("payments-api", "payment:take", "Payment");

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, ordersBenzene)
            .MapGet("https://orders-api.example/spec?type=asyncapi&format=json", HttpStatusCode.OK, ordersAsyncApi)
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true))
            .MapGet(paymentsSpecUrl, HttpStatusCode.OK, paymentsBenzene)
            .MapGet("https://payments-api.example/spec?type=asyncapi&format=json", HttpStatusCode.OK, paymentsAsyncApi)
            .MapGet(paymentsHealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl),
            new MeshServiceRegistryEntry("payments-api", paymentsSpecUrl, paymentsHealthUrl),
        }));

        var json = await store.TryReadAsync("asyncapi.json");
        Assert.NotNull(json);

        // Both services' channels + operations are present, namespaced by service, and the reserved
        // "spec" topic is filtered out. (Real AsyncAPI-reader validity is proven in an isolated project -
        // see AsyncApiCompositorTest's note on the YamlDotNet conflict.)
        var parsed = JsonNode.Parse(json!)!;
        Assert.Equal("3.0.0", parsed["asyncapi"]!.GetValue<string>());
        var channels = parsed["channels"]!.AsObject();
        Assert.True(channels.ContainsKey("orders-api_order_create"));
        Assert.True(channels.ContainsKey("payments-api_payment_take"));
        Assert.DoesNotContain(channels, kv => kv.Key.Contains("spec"));
        var operations = parsed["operations"]!.AsObject();
        Assert.True(operations.ContainsKey("orders-api_order_create"));
        Assert.True(operations.ContainsKey("payments-api_payment_take"));
    }

    private static string AsyncApiDoc(string title, string topic, string schema)
    {
        var ch = topic.Replace(":", "_");
        return $$"""
        {
          "asyncapi": "3.0.0",
          "info": { "title": "{{title}}", "version": "1.0" },
          "channels": {
            "{{ch}}": { "address": "{{topic}}", "messages": { "{{schema}}": { "payload": { "$ref": "#/components/schemas/{{schema}}" } } } },
            "spec": { "address": "spec", "messages": {} }
          },
          "operations": {
            "{{ch}}": { "action": "receive", "channel": { "$ref": "#/channels/{{ch}}" }, "messages": [ { "$ref": "#/channels/{{ch}}/messages/{{schema}}" } ] },
            "spec": { "action": "receive", "channel": { "$ref": "#/channels/spec" } }
          },
          "components": { "schemas": { "{{schema}}": { "type": "object", "properties": { "id": { "type": "string" } } } } }
        }
        """;
    }

    [Fact]
    public async Task RunOnceAsync_TopicWithSchema_InlinesRefsAndCarriesRequestResponseSchema()
    {
        var ordersSpec = """{"requests":[{"topic":"order:create","request":{"$ref":"#/components/schemas/CreateOrder"},"response":{"$ref":"#/components/schemas/Order"}}],"components":{"schemas":{"CreateOrder":{"type":"object","required":["customerId"],"properties":{"customerId":{"type":"string","format":"uuid"},"line":{"$ref":"#/components/schemas/OrderLine"}}},"OrderLine":{"type":"object","properties":{"sku":{"type":"string","pattern":"^[A-Z]{3}$"}}},"Order":{"type":"object","properties":{"id":{"type":"string"}}}}}}""";

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, ordersSpec)
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(SingleServiceRegistry());

        var catalog = JsonSerializer.Deserialize<MeshTopicCatalog>((await store.TryReadAsync("topics.json"))!, JsonOptions)!;
        var create = Assert.Single(catalog.Topics, t => t.Topic == "order:create");

        Assert.False(create.SchemaMismatch);
        Assert.NotNull(create.RequestSchema);
        Assert.NotNull(create.ResponseSchema);

        // The top-level $ref was resolved and tagged with the ref name as a title.
        Assert.Equal("CreateOrder", create.RequestSchema!["title"]!.GetValue<string>());
        var props = create.RequestSchema["properties"]!.AsObject();
        Assert.Equal("uuid", props["customerId"]!["format"]!.GetValue<string>());

        // A nested $ref was inlined too - the "line" property is now a real object with the
        // referenced component's properties, not a dangling { "$ref": ... }.
        var line = props["line"]!.AsObject();
        Assert.Null(line["$ref"]);
        Assert.Equal("OrderLine", line["title"]!.GetValue<string>());
        Assert.Equal("^[A-Z]{3}$", line["properties"]!["sku"]!["pattern"]!.GetValue<string>());
    }

    [Fact]
    public async Task RunOnceAsync_TwoConsumersSameTopicVersion_DifferentPayloads_FlagsSchemaMismatch()
    {
        const string paymentsSpecUrl = "https://payments-api.example/spec?type=benzene";
        const string paymentsHealthUrl = "https://payments-api.example/healthcheck";

        // Both services consume order:submitted (unversioned), but with different request payloads -
        // orders-api expects { id }, fulfilment-api expects { id, warehouse }. Same topic + version,
        // divergent contract: a likely error the mesh should surface.
        var ordersSpec = """{"requests":[{"topic":"order:submitted","request":{"$ref":"#/components/schemas/S"}}],"components":{"schemas":{"S":{"type":"object","properties":{"id":{"type":"string"}}}}}}""";
        var fulfilmentSpec = """{"requests":[{"topic":"order:submitted","request":{"$ref":"#/components/schemas/S"}}],"components":{"schemas":{"S":{"type":"object","properties":{"id":{"type":"string"},"warehouse":{"type":"string"}}}}}}""";

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, ordersSpec)
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true))
            .MapGet(paymentsSpecUrl, HttpStatusCode.OK, fulfilmentSpec)
            .MapGet(paymentsHealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl),
            new MeshServiceRegistryEntry("fulfilment-api", paymentsSpecUrl, paymentsHealthUrl),
        }));

        var catalog = JsonSerializer.Deserialize<MeshTopicCatalog>((await store.TryReadAsync("topics.json"))!, JsonOptions)!;
        var submitted = Assert.Single(catalog.Topics, t => t.Topic == "order:submitted");

        Assert.Equal(2, submitted.Consumers.Length);
        Assert.True(submitted.SchemaMismatch);
        Assert.NotNull(submitted.RequestSchema); // one consumer's schema is still shown for reference
    }

    [Fact]
    public async Task RunOnceAsync_TwoConsumersSameTopicVersion_IdenticalPayloads_NoMismatch()
    {
        const string paymentsSpecUrl = "https://payments-api.example/spec?type=benzene";
        const string paymentsHealthUrl = "https://payments-api.example/healthcheck";

        // Same payload declared by both consumers, even down to a different property order in the
        // component - the key-order-normalized comparison must treat these as equal.
        var ordersSpec = """{"requests":[{"topic":"order:submitted","request":{"$ref":"#/components/schemas/S"}}],"components":{"schemas":{"S":{"type":"object","properties":{"id":{"type":"string"},"total":{"type":"integer"}}}}}}""";
        var fulfilmentSpec = """{"requests":[{"topic":"order:submitted","request":{"$ref":"#/components/schemas/S"}}],"components":{"schemas":{"S":{"properties":{"total":{"type":"integer"},"id":{"type":"string"}},"type":"object"}}}}""";

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, ordersSpec)
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true))
            .MapGet(paymentsSpecUrl, HttpStatusCode.OK, fulfilmentSpec)
            .MapGet(paymentsHealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl),
            new MeshServiceRegistryEntry("fulfilment-api", paymentsSpecUrl, paymentsHealthUrl),
        }));

        var catalog = JsonSerializer.Deserialize<MeshTopicCatalog>((await store.TryReadAsync("topics.json"))!, JsonOptions)!;
        var submitted = Assert.Single(catalog.Topics, t => t.Topic == "order:submitted");

        Assert.Equal(2, submitted.Consumers.Length);
        Assert.False(submitted.SchemaMismatch);
    }

    [Fact]
    public async Task RunOnceAsync_ProducerEvent_CarriesMessageSchema()
    {
        var ordersSpec = """{"events":[{"topic":"order:shipped","message":{"$ref":"#/components/schemas/Shipped"}}],"components":{"schemas":{"Shipped":{"type":"object","properties":{"trackingId":{"type":"string"}}}}}}""";

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, ordersSpec)
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(SingleServiceRegistry());

        var catalog = JsonSerializer.Deserialize<MeshTopicCatalog>((await store.TryReadAsync("topics.json"))!, JsonOptions)!;
        var shipped = Assert.Single(catalog.Topics, t => t.Topic == "order:shipped");

        Assert.NotNull(shipped.MessageSchema);
        Assert.Equal("Shipped", shipped.MessageSchema!["title"]!.GetValue<string>());
        Assert.Equal("string", shipped.MessageSchema["properties"]!["trackingId"]!["type"]!.GetValue<string>());
        Assert.False(shipped.SchemaMismatch); // a single producer, no consumers to disagree
    }

    [Fact]
    public async Task RunOnceAsync_TopicCatalog_KeysByVersion_AndFlagsDeprecationCandidateAndGap()
    {
        const string paymentsSpecUrl = "https://payments-api.example/spec?type=benzene";
        const string paymentsHealthUrl = "https://payments-api.example/healthcheck";

        // orders-api: sends shipping:booked@v1 (events) - nobody handles it anywhere -> deprecation
        // candidate. Also sends shipping:booked@v2, which payments-api does handle -> unflagged.
        var ordersSpec = "{\"requests\":[],\"events\":["
            + "{\"topic\":\"shipping:booked\",\"version\":\"v1\"},"
            + "{\"topic\":\"shipping:booked\",\"version\":\"v2\"}]}";
        // payments-api: handles shipping:booked@v2 (a consumer, no HTTP mapping - purely
        // queue-style) and separately handles legacy:refund with no producer anywhere -> gap.
        var paymentsSpec = "{\"requests\":["
            + "{\"topic\":\"shipping:booked\",\"version\":\"v2\"},"
            + "{\"topic\":\"legacy:refund\"}],\"events\":[]}";

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, ordersSpec)
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true))
            .MapGet(paymentsSpecUrl, HttpStatusCode.OK, paymentsSpec)
            .MapGet(paymentsHealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl),
            new MeshServiceRegistryEntry("payments-api", paymentsSpecUrl, paymentsHealthUrl),
        }));

        var json = await store.TryReadAsync("topics.json");
        var catalog = JsonSerializer.Deserialize<MeshTopicCatalog>(json!, JsonOptions)!;

        // v1 and v2 of the same topic id are distinct entries.
        var bookedV1 = Assert.Single(catalog.Topics, t => t.Topic == "shipping:booked" && t.Version == "v1");
        var bookedV2 = Assert.Single(catalog.Topics, t => t.Topic == "shipping:booked" && t.Version == "v2");

        // v1: produced, never consumed anywhere in the fleet -> deprecation candidate.
        Assert.Equal("orders-api", Assert.Single(bookedV1.Producers).Service);
        Assert.Empty(bookedV1.Consumers);
        Assert.Equal(MeshTopicStatus.DeprecationCandidate, bookedV1.Status);

        // v2: produced by orders-api, consumed by payments-api -> unflagged, both sides present.
        Assert.Equal("orders-api", Assert.Single(bookedV2.Producers).Service);
        Assert.Equal("payments-api", Assert.Single(bookedV2.Consumers).Service);
        Assert.Null(bookedV2.Status);

        // legacy:refund: consumed by payments-api with no HTTP mapping, produced nowhere in the
        // fleet -> gap (informational - may legitimately come from outside this fleet).
        var refund = Assert.Single(catalog.Topics, t => t.Topic == "legacy:refund");
        Assert.Empty(refund.Producers);
        Assert.Equal("payments-api", Assert.Single(refund.Consumers).Service);
        Assert.Equal(MeshTopicStatus.Gap, refund.Status);
    }

    [Fact]
    public async Task RunOnceAsync_DerivesStructuralTopology_FromSenderToHandler()
    {
        const string paymentsSpecUrl = "https://payments-api.example/spec?type=benzene";
        const string paymentsHealthUrl = "https://payments-api.example/healthcheck";
        // orders declares it *sends* payments:capture (events); payments *handles* it (requests).
        var ordersSpec = "{\"requests\":[{\"topic\":\"orders:create\"}],\"events\":[{\"topic\":\"payments:capture\"}]}";
        var paymentsSpec = "{\"requests\":[{\"topic\":\"payments:capture\"}]}";

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, ordersSpec)
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true))
            .MapGet(paymentsSpecUrl, HttpStatusCode.OK, paymentsSpec)
            .MapGet(paymentsHealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl),
            new MeshServiceRegistryEntry("payments-api", paymentsSpecUrl, paymentsHealthUrl),
        }));

        var json = await store.TryReadAsync("topology.json");
        Assert.NotNull(json);
        var topology = JsonSerializer.Deserialize<MeshTopology>(json!, JsonOptions)!;

        var edge = Assert.Single(topology.Edges);
        Assert.Equal("orders-api", edge.Client);
        Assert.Equal("payments-api", edge.Server);
        Assert.Equal(TopologyEdgeSource.Structural, edge.Source);
    }

    [Fact]
    public async Task RunOnceAsync_UnchangedSpec_SecondRun_NoDrift()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(SingleServiceRegistry());
        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.False(Assert.Single(manifest.Services).ContractDrift);
    }

    [Fact]
    public async Task RunOnceAsync_ChangedSpec_SecondRun_ReportsDrift()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(SingleServiceRegistry());

        handler.MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\",\"version\":\"2\"}}");
        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.True(Assert.Single(manifest.Services).ContractDrift);
    }

    [Fact]
    public async Task RunOnceAsync_RegistryEntryHasOwningTeam_ThreadsThroughToManifest()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var registry = new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl, MeshServiceSource.Http, null, "team-checkout"),
        });

        var manifest = await aggregator.RunOnceAsync(registry);

        Assert.Equal("team-checkout", Assert.Single(manifest.Services).OwningTeam);
    }

    [Fact]
    public async Task RunOnceAsync_RegistryEntryHasNoOwningTeam_ManifestOwningTeamIsNull()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Null(Assert.Single(manifest.Services).OwningTeam);
    }

    [Fact]
    public async Task RunOnceAsync_SpecAdvertisesTransports_ThreadsThroughToManifest()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"},\"transports\":[\"sqs\",\"http\"]}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Equal(new[] { "sqs", "http" }, Assert.Single(manifest.Services).Transports);
    }

    [Fact]
    public async Task RunOnceAsync_SpecHasNoTransports_ManifestTransportsIsEmpty()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Empty(Assert.Single(manifest.Services).Transports);
    }

    [Fact]
    public async Task RunOnceAsync_HealthEndpointReportsUnhealthy_ManifestShowsUnhealthy()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(false));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Equal(MeshServiceStatus.Unhealthy, Assert.Single(manifest.Services).Status);
    }

    [Fact]
    public async Task RunOnceAsync_HealthEndpointReports503Unhealthy_ManifestShowsUnhealthyNotUnreachable()
    {
        // Benzene.HealthChecks.HealthCheckProcessor.PerformHealthChecksAsync deliberately maps an
        // unhealthy aggregate result to HTTP 503 (ServiceUnavailable), not 200 - a real Benzene
        // health check reports "unhealthy" this way, not via a 200 with isHealthy:false in the
        // body. The aggregator must still read and deserialize that body instead of treating the
        // non-2xx status as an unreachable/fetch-failure case.
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.ServiceUnavailable, SerializeHealth(false));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Equal(MeshServiceStatus.Unhealthy, Assert.Single(manifest.Services).Status);
    }

    [Fact]
    public async Task RunOnceAsync_BothEndpointsFail_ManifestShowsUnreachable_ErrorIsTypeNameOnly()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.InternalServerError, "connection string: secret-value")
            .MapGet(HealthUrl, HttpStatusCode.InternalServerError, "connection string: secret-value");
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Equal(MeshServiceStatus.Unreachable, Assert.Single(manifest.Services).Status);

        var snapshotJson = await store.TryReadAsync("services/orders-api.json");
        Assert.NotNull(snapshotJson);
        var snapshot = JsonSerializer.Deserialize<MeshServiceSnapshot>(snapshotJson!, JsonOptions);
        Assert.NotNull(snapshot!.Error);
        Assert.DoesNotContain("secret-value", snapshot.Error);
        Assert.Equal(nameof(HttpRequestException), snapshot.Error);
    }

    [Fact]
    public async Task RunOnceAsync_OneServiceUnreachable_OtherHealthy_BothPublished()
    {
        const string otherSpecUrl = "https://payments-api.example/spec?type=benzene";
        const string otherHealthUrl = "https://payments-api.example/healthcheck";

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.InternalServerError, null)
            .MapGet(HealthUrl, HttpStatusCode.InternalServerError, null)
            .MapGet(otherSpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"payments-api\"}}")
            .MapGet(otherHealthUrl, HttpStatusCode.OK, SerializeHealth(true));

        var registry = new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl),
            new MeshServiceRegistryEntry("payments-api", otherSpecUrl, otherHealthUrl),
        });
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory));

        var manifest = await aggregator.RunOnceAsync(registry);

        Assert.Equal(2, manifest.Services.Length);
        Assert.Equal(MeshServiceStatus.Unreachable, manifest.Services.Single(x => x.Name == "orders-api").Status);
        Assert.Equal(MeshServiceStatus.Healthy, manifest.Services.Single(x => x.Name == "payments-api").Status);
    }

    [Fact]
    public async Task RunOnceAsync_EntryUsesNonHttpSource_ResolvesFromRegisteredSource_NotHttpClient()
    {
        // Proves the IMeshServiceSource seam is real: an entry with Source="fake" is fetched via
        // FakeMeshServiceSource below, never touching the HttpClient-backed HttpMeshServiceSource
        // also registered here - if MeshAggregator still had the fetch inlined, this would fail
        // (no stub configured for the fake entry's SpecUrl/HealthUrl on the HTTP handler).
        var handler = new RoutingHttpMessageHandler();
        var fakeSource = new FakeMeshServiceSource(
            "{\"info\":{\"title\":\"orders-api\"}}", SerializeHealth(true));
        var aggregator = new MeshAggregator(
            new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)), fakeSource },
            new FileSystemMeshArtifactStore(_rootDirectory));

        var registry = new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl, "fake", null),
        });

        var manifest = await aggregator.RunOnceAsync(registry);

        Assert.Equal(MeshServiceStatus.Healthy, Assert.Single(manifest.Services).Status);
        Assert.True(fakeSource.WasCalled);
    }

    [Fact]
    public async Task RunOnceAsync_EntryUsesUnregisteredSource_ManifestShowsUnreachable_DoesNotCrashOtherServices()
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var aggregator = new MeshAggregator(
            new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) },
            new FileSystemMeshArtifactStore(_rootDirectory));

        var registry = new MeshServiceRegistry(new[]
        {
            new MeshServiceRegistryEntry("orders-api", SpecUrl, HealthUrl),
            new MeshServiceRegistryEntry("payments-fn", "n/a", "n/a", "AwsLambdaInvoke", null),
        });

        var manifest = await aggregator.RunOnceAsync(registry);

        Assert.Equal(2, manifest.Services.Length);
        Assert.Equal(MeshServiceStatus.Healthy, manifest.Services.Single(x => x.Name == "orders-api").Status);
        Assert.Equal(MeshServiceStatus.Unreachable, manifest.Services.Single(x => x.Name == "payments-fn").Status);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private class RoutingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode StatusCode, string? Content)> _responses = new();

        public RoutingHttpMessageHandler MapGet(string url, HttpStatusCode statusCode, string? content)
        {
            _responses[url] = (statusCode, content);
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            if (!_responses.TryGetValue(url, out var response))
            {
                throw new InvalidOperationException($"No stubbed response configured for {url}");
            }

            var message = new HttpResponseMessage(response.StatusCode);
            if (response.Content != null)
            {
                message.Content = new StringContent(response.Content);
            }

            return Task.FromResult(message);
        }
    }

    private class FakeMeshServiceSource : IMeshServiceSource
    {
        private readonly string _specJson;
        private readonly string _healthJson;

        public FakeMeshServiceSource(string specJson, string healthJson)
        {
            _specJson = specJson;
            _healthJson = healthJson;
        }

        public string Key => "fake";

        public bool WasCalled { get; private set; }

        public Task<string> FetchSpecAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(_specJson);
        }

        public Task<string> FetchHealthAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken)
        {
            return Task.FromResult(_healthJson);
        }
    }
}
