using Benzene.Schema.OpenApi.EventService;
using Benzene.Schema.OpenApi.Examples;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Benzene.Schema.OpenApi.TestPayloads;

/// <summary>
/// Builds a manifest of ready-to-fire example payloads for a service's domain topics from its own
/// <see cref="EventServiceDocument"/>. For each topic it emits the deterministic, validation-aware
/// example body (the same generator the spec uses) wrapped in the transport-agnostic BenzeneMessage
/// envelope - the exact shape a caller POSTs to the service's <c>/benzene-message</c> endpoint - so a
/// deployed service can self-serve "here is how you call me, and here is a valid example".
/// </summary>
/// <remarks>
/// This runtime core deliberately depends only on the runtime-safe example generator
/// (<see cref="Examples.ExamplePayloadBuilder"/>/<see cref="Examples.SchemaGetter"/>) and carries no
/// AWS coupling. It reports the transports each topic is reachable on (the host's wired
/// <see cref="EventServiceDocument.Transports"/> plus the topic's own HTTP mappings) but dresses only
/// the portable BenzeneMessage payload; per-transport AWS event envelopes (SNS/SQS/API Gateway) are a
/// separate opt-in concern (see <c>work/runtime-test-payloads-plan.md</c>).
/// </remarks>
public class TestPayloadsBuilder
{
    private const string BenzeneMessageTransport = "benzene-message";

    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
    };

    private readonly IExamplePayloadBuilder _examplePayloadBuilder;
    private readonly IReadOnlyList<ITestPayloadDresser> _dressers;

    /// <summary>Initializes a new instance using the default example generator and no transport dressers.</summary>
    public TestPayloadsBuilder()
        : this(new ExamplePayloadBuilder(), null)
    {
    }

    /// <summary>Initializes a new instance using the default example generator.</summary>
    /// <param name="examplePayloadBuilder">The example generator used for payload bodies.</param>
    public TestPayloadsBuilder(IExamplePayloadBuilder examplePayloadBuilder)
        : this(examplePayloadBuilder, null)
    {
    }

    /// <summary>Initializes a new instance with transport dressers (default example generator).</summary>
    /// <param name="dressers">
    /// Per-transport dressers that add SNS/SQS/API-Gateway (etc.) payloads alongside the always-present
    /// portable <c>benzene-message</c> envelope. Registered from opt-in transport packages.
    /// </param>
    public TestPayloadsBuilder(IEnumerable<ITestPayloadDresser> dressers)
        : this(new ExamplePayloadBuilder(), dressers)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="examplePayloadBuilder">The example generator used for payload bodies.</param>
    /// <param name="dressers">Per-transport dressers, or <c>null</c> for the portable envelope only.</param>
    public TestPayloadsBuilder(IExamplePayloadBuilder examplePayloadBuilder, IEnumerable<ITestPayloadDresser>? dressers)
    {
        _examplePayloadBuilder = examplePayloadBuilder;
        _dressers = dressers?.ToArray() ?? Array.Empty<ITestPayloadDresser>();
    }

    /// <summary>Builds the manifest for every domain topic (reserved utility topics are skipped).</summary>
    /// <param name="document">The service's event-service document.</param>
    /// <param name="topicFilter">When non-null, restricts the manifest to this single topic.</param>
    public TestPayloadsManifest Build(EventServiceDocument document, string? topicFilter = null)
    {
        var schemaGetter = new SchemaGetter(document.Components.Schemas);

        var topics = document.Requests
            .Where(request => !ReservedTopics.IsReserved(request.Topic))
            .Where(request => topicFilter == null || string.Equals(request.Topic, topicFilter, StringComparison.OrdinalIgnoreCase))
            .Select(request => BuildTopic(document, request, schemaGetter))
            .ToArray();

        return new TestPayloadsManifest { Topics = topics };
    }

    /// <summary>Builds the manifest and serializes it to indented, camelCased JSON.</summary>
    public string BuildJson(EventServiceDocument document, string? topicFilter = null)
    {
        return JsonConvert.SerializeObject(Build(document, topicFilter), SerializerSettings);
    }

    private TestPayloadTopic BuildTopic(EventServiceDocument document, RequestResponse request, ISchemaGetter schemaGetter)
    {
        var body = _examplePayloadBuilder.Build(request.Request, schemaGetter);
        var serializedBody = JsonConvert.SerializeObject(body);
        var transports = TransportsFor(document, request);
        var httpMappings = request.HttpMappings
            .Select(mapping => new TestPayloadHttpMapping { Method = mapping.Method, Path = mapping.Path })
            .ToArray();

        var payloads = new Dictionary<string, object>
        {
            // The portable BenzeneMessage envelope - POST this to the service's /benzene-message
            // endpoint to invoke the topic. Matches the "benzene-message" Lambda-test-tool dressing.
            [BenzeneMessageTransport] = new BenzeneMessagePayload
            {
                Topic = request.Topic,
                Headers = new Dictionary<string, string>(),
                Body = serializedBody,
            },
        };

        // Fold in any registered transport dressers (SNS/SQS/API-Gateway from opt-in packages). Each
        // decides its own applicability (returns null to skip - e.g. api-gateway for a non-HTTP topic),
        // reusing the same serialized body so every transport agrees on the payload.
        if (_dressers.Count > 0)
        {
            var context = new TestPayloadDressingContext(
                request.Topic,
                new Dictionary<string, string>(),
                serializedBody,
                transports,
                httpMappings);

            foreach (var dresser in _dressers)
            {
                var dressed = dresser.Dress(context);
                if (dressed != null)
                {
                    payloads[dresser.Transport] = dressed;
                }
            }
        }

        return new TestPayloadTopic
        {
            Topic = request.Topic,
            Transports = transports,
            HttpMappings = httpMappings,
            Payloads = payloads,
        };
    }

    // Every wired non-HTTP transport reaches every topic (Benzene has no per-topic transport
    // filtering), so those come from the document; HTTP reachability is the per-topic exception,
    // present only when the topic has an [HttpEndpoint] mapping.
    private static string[] TransportsFor(EventServiceDocument document, RequestResponse request)
    {
        var transports = new List<string>(document.Transports);
        if (request.HttpMappings.Length > 0 && !transports.Contains("http", StringComparer.OrdinalIgnoreCase))
        {
            transports.Add("http");
        }

        return transports.ToArray();
    }
}
