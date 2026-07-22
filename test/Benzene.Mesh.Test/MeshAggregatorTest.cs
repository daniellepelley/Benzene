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
    public async Task RunOnceAsync_PopulatesSnapshotAtUtc_FromTheSnapshotFetchTime()
    {
        // The manifest row carries a per-service freshness timestamp (denormalized from the snapshot's
        // FetchedAtUtc) so a catalog/issue view can judge staleness from manifest.json alone.
        var at = new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory), () => at);

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Equal(at, Assert.Single(manifest.Services).SnapshotAtUtc);
    }

    [Fact]
    public async Task RunOnceAsync_UnreachableService_StillCarriesSnapshotAtUtc()
    {
        // Staleness must work precisely for services that stopped responding, so an unreachable row
        // still ages honestly - its SnapshotAtUtc is populated even though the fetch failed.
        var at = new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.InternalServerError, null)
            .MapGet(HealthUrl, HttpStatusCode.InternalServerError, null);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, new FileSystemMeshArtifactStore(_rootDirectory), () => at);

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        var entry = Assert.Single(manifest.Services);
        Assert.Equal(MeshServiceStatus.Unreachable, entry.Status);
        Assert.Equal(at, entry.SnapshotAtUtc);
    }

    [Fact]
    public void MeshManifestEntry_SnapshotAtUtc_RoundTrips_AndIsBackwardCompatible()
    {
        var at = new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);
        var manifest = new MeshManifest(at, new[]
        {
            new MeshManifestEntry("orders-api", MeshServiceStatus.Healthy, false, SpecUrl, HealthUrl,
                owningTeam: null, transports: null, snapshotAtUtc: at),
        });

        var round = JsonSerializer.Deserialize<MeshManifest>(JsonSerializer.Serialize(manifest, JsonOptions), JsonOptions)!;
        Assert.Equal(at, Assert.Single(round.Services).SnapshotAtUtc);

        // An older manifest.json without the field deserializes with a null timestamp (back-compat).
        var legacy = """{"generatedAtUtc":"2026-07-20T09:00:00+00:00","services":[{"name":"orders-api","status":"healthy","contractDrift":false,"specUrl":"s","healthUrl":"h"}]}""";
        var legacyManifest = JsonSerializer.Deserialize<MeshManifest>(legacy, JsonOptions)!;
        Assert.Null(Assert.Single(legacyManifest.Services).SnapshotAtUtc);
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
    public async Task RunOnceAsync_ReconcilesVersions_AcrossProducersAndConsumers()
    {
        const string paymentsSpecUrl = "https://payments-api.example/spec?type=benzene";
        const string paymentsHealthUrl = "https://payments-api.example/healthcheck";

        // orders-api DECLARES it produces payments:get at version "1" (spec events); payments-api HANDLES
        // payments:get at version "2" (a single v2 handler). Structurally that's a skew: v1 is emitted but
        // no service handles v1 - the exact "producer emits vN, is anyone consuming vN?" question. (At
        // runtime an upcaster on payments bridges it, which the mesh can't see - hence a "look" signal.)
        var ordersSpec = "{\"requests\":[{\"topic\":\"order:create\"}],\"events\":[{\"topic\":\"payments:get\",\"version\":\"1\"}]}";
        var paymentsSpec = "{\"requests\":[{\"topic\":\"payments:get\",\"version\":\"2\"}]}";

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

        var catalog = JsonSerializer.Deserialize<MeshTopicCatalog>((await store.TryReadAsync("topics.json"))!, JsonOptions)!;

        var compat = Assert.Single(catalog.VersionCompatibility, v => v.Topic == "payments:get");
        Assert.Equal(new[] { "1" }, compat.ProducedVersions);
        Assert.Equal(new[] { "2" }, compat.ConsumedVersions);
        Assert.Equal(new[] { "1" }, compat.ProducedNotConsumed); // v1 emitted, nobody handles v1
        Assert.Equal(new[] { "2" }, compat.ConsumedNotProduced); // v2 handled, nobody emits v2
        Assert.False(compat.IsCompatible);

        // A single-version, single-side topic (order:create) has no compatibility question, so no entry.
        Assert.DoesNotContain(catalog.VersionCompatibility, v => v.Topic == "order:create");
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
    public async Task RunOnceAsync_PolymorphicSchema_InlinesRefsInsideOneOfAndAllOfBranches()
    {
        // A polymorphic contract: the request's "payment" is a oneOf union whose branches are
        // $refs, and each derived component composes its base via allOf [$ref]. Every ref must be
        // inlined so topics.json stays self-contained (no dangling refs for the UI to choke on).
        var ordersSpec = """{"requests":[{"topic":"order:pay","request":{"$ref":"#/components/schemas/PayOrder"}}],"components":{"schemas":{"PayOrder":{"type":"object","properties":{"payment":{"oneOf":[{"$ref":"#/components/schemas/CardPayment"},{"$ref":"#/components/schemas/BankPayment"}]}}},"CardPayment":{"type":"object","allOf":[{"$ref":"#/components/schemas/PaymentMethod"}],"properties":{"cardNumber":{"type":"string"}}},"BankPayment":{"type":"object","allOf":[{"$ref":"#/components/schemas/PaymentMethod"}],"properties":{"iban":{"type":"string"}}},"PaymentMethod":{"type":"object","properties":{"currency":{"type":"string"}}}}}}""";

        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, ordersSpec)
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(SingleServiceRegistry());

        var catalog = JsonSerializer.Deserialize<MeshTopicCatalog>((await store.TryReadAsync("topics.json"))!, JsonOptions)!;
        var pay = Assert.Single(catalog.Topics, t => t.Topic == "order:pay");

        var payment = pay.RequestSchema!["properties"]!["payment"]!.AsObject();
        var oneOf = payment["oneOf"]!.AsArray();
        Assert.Equal(2, oneOf.Count);

        // Each oneOf branch is the inlined component (title-tagged), not a dangling $ref...
        var card = oneOf[0]!.AsObject();
        Assert.Null(card["$ref"]);
        Assert.Equal("CardPayment", card["title"]!.GetValue<string>());
        Assert.Equal("string", card["properties"]!["cardNumber"]!["type"]!.GetValue<string>());

        // ...and refs nested inside a branch's allOf are inlined too.
        var cardBase = card["allOf"]!.AsArray()[0]!.AsObject();
        Assert.Null(cardBase["$ref"]);
        Assert.Equal("PaymentMethod", cardBase["title"]!.GetValue<string>());
        Assert.Equal("string", cardBase["properties"]!["currency"]!["type"]!.GetValue<string>());
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
    public async Task RunOnceAsync_SameRequest_OneConsumerDeclaresResponseOtherDoesnt_NoMismatch()
    {
        const string paymentsSpecUrl = "https://payments-api.example/spec?type=benzene";
        const string paymentsHealthUrl = "https://payments-api.example/healthcheck";

        // Both consumers declare the SAME request payload. orders-api also declares a response;
        // fulfilment-api declares none. "No response declared" is no-signal, not a contradiction, so
        // this must NOT be a schema mismatch (it used to be, because a null response was folded into
        // the compare string and differed from orders-api's declared one).
        var ordersSpec = """{"requests":[{"topic":"order:submitted","request":{"$ref":"#/components/schemas/S"},"response":{"$ref":"#/components/schemas/R"}}],"components":{"schemas":{"S":{"type":"object","properties":{"id":{"type":"string"}}},"R":{"type":"object","properties":{"ok":{"type":"boolean"}}}}}}""";
        var fulfilmentSpec = """{"requests":[{"topic":"order:submitted","request":{"$ref":"#/components/schemas/S"}}],"components":{"schemas":{"S":{"type":"object","properties":{"id":{"type":"string"}}}}}}""";

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

    private MeshAggregator CatalogDiffAggregator(string specJson, IMeshArtifactStore store)
    {
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, specJson)
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        return new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);
    }

    private async Task<MeshTopicCatalog> ReadCatalogAsync(IMeshArtifactStore store)
    {
        return JsonSerializer.Deserialize<MeshTopicCatalog>((await store.TryReadAsync("topics.json"))!, JsonOptions)!;
    }

    [Fact]
    public async Task RunOnceAsync_FirstRun_ClaimsNoTopicChanges()
    {
        // With no previous topics.json there is nothing to diff against - a first run must not
        // render the whole estate as a wall of "topic-added" noise.
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        await CatalogDiffAggregator("""{"requests":[{"topic":"order:create"}]}""", store).RunOnceAsync(SingleServiceRegistry());

        var catalog = await ReadCatalogAsync(store);

        Assert.Empty(Assert.Single(catalog.Topics, t => t.Topic == "order:create").Changes);
        Assert.Empty(catalog.RemovedTopics);
    }

    [Fact]
    public async Task RunOnceAsync_SecondRun_FlagsANewlyDeclaredTopic()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        await CatalogDiffAggregator("""{"requests":[{"topic":"order:create"}]}""", store).RunOnceAsync(SingleServiceRegistry());
        await CatalogDiffAggregator("""{"requests":[{"topic":"order:create"},{"topic":"order:refund"}]}""", store).RunOnceAsync(SingleServiceRegistry());

        var catalog = await ReadCatalogAsync(store);

        var refund = Assert.Single(catalog.Topics, t => t.Topic == "order:refund");
        Assert.Equal(MeshTopicChangeKind.Added, Assert.Single(refund.Changes).Kind);
        // The unchanged topic carries no changes - the diff is per-entry, not run-wide.
        Assert.Empty(Assert.Single(catalog.Topics, t => t.Topic == "order:create").Changes);
    }

    [Fact]
    public async Task RunOnceAsync_SecondRun_FlagsASchemaChangeWithTheChangedSide()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        await CatalogDiffAggregator(
            """{"requests":[{"topic":"order:create","request":{"type":"object","properties":{"id":{"type":"string"}}}}]}""",
            store).RunOnceAsync(SingleServiceRegistry());
        await CatalogDiffAggregator(
            """{"requests":[{"topic":"order:create","request":{"type":"object","properties":{"id":{"type":"integer"}}}}]}""",
            store).RunOnceAsync(SingleServiceRegistry());

        var catalog = await ReadCatalogAsync(store);

        var change = Assert.Single(Assert.Single(catalog.Topics, t => t.Topic == "order:create").Changes);
        Assert.Equal(MeshTopicChangeKind.SchemaChanged, change.Kind);
        Assert.Contains("request", change.Description);
    }

    [Fact]
    public async Task RunOnceAsync_SecondRun_FlagsAConsumerSetChange()
    {
        // The same topic handled by a differently-named service across runs (a rename, or a
        // handoff between services) is a consumer-set change worth reviewing, not silence.
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var spec = """{"requests":[{"topic":"order:create"}]}""";
        await CatalogDiffAggregator(spec, store).RunOnceAsync(SingleServiceRegistry());
        await CatalogDiffAggregator(spec, store).RunOnceAsync(
            new MeshServiceRegistry(new[] { new MeshServiceRegistryEntry("orders-api-v2", SpecUrl, HealthUrl) }));

        var catalog = await ReadCatalogAsync(store);

        var change = Assert.Single(Assert.Single(catalog.Topics, t => t.Topic == "order:create").Changes);
        Assert.Equal(MeshTopicChangeKind.ConsumersChanged, change.Kind);
        Assert.Contains("+orders-api-v2", change.Description);
        Assert.Contains("-orders-api", change.Description);
    }

    [Fact]
    public async Task RunOnceAsync_SecondRun_RecordsAVanishedTopicOnTheCatalog()
    {
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        await CatalogDiffAggregator("""{"requests":[{"topic":"order:create"},{"topic":"order:legacy-export","version":"v1"}]}""", store)
            .RunOnceAsync(SingleServiceRegistry());
        await CatalogDiffAggregator("""{"requests":[{"topic":"order:create"}]}""", store).RunOnceAsync(SingleServiceRegistry());

        var catalog = await ReadCatalogAsync(store);

        var removed = Assert.Single(catalog.RemovedTopics);
        Assert.Equal("order:legacy-export", removed.Topic);
        Assert.Equal("v1", removed.Version);
        Assert.DoesNotContain(catalog.Topics, t => t.Topic == "order:legacy-export");
    }

    [Fact]
    public async Task RunOnceAsync_SecondRun_ReservedTopicChurnIsNeverFlagged()
    {
        // Utility topics (spec/health/...) churn with framework versions, not with anyone's domain
        // contract - the same carve-out Status and SchemaMismatch already apply.
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        await CatalogDiffAggregator("""{"requests":[{"topic":"spec","reserved":true}]}""", store).RunOnceAsync(SingleServiceRegistry());
        await CatalogDiffAggregator("""{"requests":[{"topic":"spec","reserved":true},{"topic":"healthcheck","reserved":true}]}""", store)
            .RunOnceAsync(SingleServiceRegistry());

        var catalog = await ReadCatalogAsync(store);

        Assert.All(catalog.Topics, t => Assert.Empty(t.Changes));
    }

    [Fact]
    public async Task RunOnceAsync_NoUsageSources_PublishesNoUsageArtifact()
    {
        // usage.json's absence must keep meaning "no usage feed wired" - a run without any
        // registered IMeshUsageSource never writes the artifact, so the UI hides its usage surfaces.
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store);

        await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Null(await store.TryReadAsync("usage.json"));
    }

    [Fact]
    public async Task RunOnceAsync_UsageSourceReports_PublishesUsageJson()
    {
        var at = new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero);
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var report = new MeshUsage(at.AddMinutes(-1), at.AddDays(-1), at,
            new[] { new MeshUsageEntry("orders:create", "v1", "orders-api", "Sqs", "Created", 42, 12.5, "stub") });
        var aggregator = new MeshAggregator(
            new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store, () => at,
            new IMeshUsageSource[] { new StubUsageSource(report) });

        await aggregator.RunOnceAsync(SingleServiceRegistry());

        var usage = JsonSerializer.Deserialize<MeshUsage>((await store.TryReadAsync("usage.json"))!, JsonOptions)!;
        Assert.Equal(at, usage.GeneratedAtUtc); // the merged report is stamped by the run, not the source
        Assert.Equal(at.AddDays(-1), usage.WindowStartUtc);
        var entry = Assert.Single(usage.Entries);
        Assert.Equal("orders:create", entry.Topic);
        Assert.Equal("Sqs", entry.Transport);
        Assert.Equal(42, entry.Count);
        Assert.Equal("stub", entry.Source);
    }

    [Fact]
    public async Task RunOnceAsync_MergesUsageSources_ConcatenatingEntriesAndWideningTheWindow()
    {
        var at = new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero);
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var narrow = new MeshUsage(at, at.AddHours(-1), at,
            new[] { new MeshUsageEntry("orders:create", null, null, "AspNet", "Created", 10, null, "a") });
        var wide = new MeshUsage(at, at.AddDays(-2), at.AddHours(-2),
            new[] { new MeshUsageEntry("orders:create", null, null, null, "Ok", 5, null, "b") });
        var aggregator = new MeshAggregator(
            new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store, () => at,
            new IMeshUsageSource[] { new StubUsageSource(narrow), new StubUsageSource(wide) });

        await aggregator.RunOnceAsync(SingleServiceRegistry());

        var usage = JsonSerializer.Deserialize<MeshUsage>((await store.TryReadAsync("usage.json"))!, JsonOptions)!;
        Assert.Equal(at.AddDays(-2), usage.WindowStartUtc);
        Assert.Equal(at, usage.WindowEndUtc);
        Assert.Equal(2, usage.Entries.Length);
        Assert.Equal(new[] { "a", "b" }, usage.Entries.Select(e => e.Source).OrderBy(s => s).ToArray());
    }

    [Fact]
    public async Task RunOnceAsync_ThrowingUsageSource_ContributesNothingWithoutFailingTheRun()
    {
        // The per-service fetch rule applies to usage adapters too: one broken backend loses its
        // own entries, never the run, and never the other adapters' entries.
        var at = new DateTimeOffset(2026, 7, 22, 9, 0, 0, TimeSpan.Zero);
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var good = new MeshUsage(at, null, null,
            new[] { new MeshUsageEntry("orders:create", null, null, null, null, 7, null, "good") });
        var aggregator = new MeshAggregator(
            new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store, () => at,
            new IMeshUsageSource[] { new StubUsageSource(new InvalidOperationException("backend down")), new StubUsageSource(good) });

        var manifest = await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Equal(MeshServiceStatus.Healthy, Assert.Single(manifest.Services).Status);
        var usage = JsonSerializer.Deserialize<MeshUsage>((await store.TryReadAsync("usage.json"))!, JsonOptions)!;
        Assert.Equal("good", Assert.Single(usage.Entries).Source);
    }

    [Fact]
    public async Task RunOnceAsync_AllUsageSourcesReturnNull_PublishesNoUsageArtifact()
    {
        // null from a source means "nothing to report this run"; if every source says so there is
        // no report at all - distinct from a wired feed reporting an empty entries array.
        var handler = new RoutingHttpMessageHandler()
            .MapGet(SpecUrl, HttpStatusCode.OK, "{\"info\":{\"title\":\"orders-api\"}}")
            .MapGet(HealthUrl, HttpStatusCode.OK, SerializeHealth(true));
        var store = new FileSystemMeshArtifactStore(_rootDirectory);
        var aggregator = new MeshAggregator(
            new IMeshServiceSource[] { new HttpMeshServiceSource(new HttpClient(handler)) }, store, null,
            new IMeshUsageSource[] { new StubUsageSource((MeshUsage?)null) });

        await aggregator.RunOnceAsync(SingleServiceRegistry());

        Assert.Null(await store.TryReadAsync("usage.json"));
    }

    private class StubUsageSource : IMeshUsageSource
    {
        private readonly MeshUsage? _report;
        private readonly Exception? _exception;

        public StubUsageSource(MeshUsage? report) => _report = report;

        public StubUsageSource(Exception exception) => _exception = exception;

        public Task<MeshUsage?> FetchUsageAsync(CancellationToken cancellationToken = default)
        {
            return _exception != null ? Task.FromException<MeshUsage?>(_exception) : Task.FromResult(_report);
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
