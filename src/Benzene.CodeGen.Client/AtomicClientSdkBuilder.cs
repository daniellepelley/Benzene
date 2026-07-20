using Benzene.CodeGen.Core;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi.Models;

namespace Benzene.CodeGen.Client;

/// <summary>
/// Generates a small, single-topic ("atomic") client per topic instead of one client covering the
/// whole service. A consumer that calls only one topic gets a client scoped to just that topic: its
/// <c>RequiredTopics</c> startup-validation array and its contract hash cover only that topic's
/// contract, so an unrelated change elsewhere on the producing service neither drags in unused
/// surface nor invalidates this client - better cohesion when a service depends on one topic rather
/// than a whole service.
/// </summary>
/// <remarks>
/// Reuses <see cref="MessageClientSdkBuilder"/> against a per-topic filtered
/// <see cref="EventServiceDocument"/> (one request, only the component schemas that request reaches),
/// so the generated client/interface/routing/DTO shapes match the service-level client exactly and
/// the contract hash falls out topic-scoped. Each topic is emitted under its own client name
/// (from <see cref="TopicMethodName"/> by default, e.g. <c>user:create</c> → <c>UserCreate</c>), so
/// one document yields one atomic client set per topic.
/// </remarks>
public class AtomicClientSdkBuilder : ICodeBuilder<EventServiceDocument>
{
    private readonly string _baseNamespace;
    private readonly IMethodName _clientNameFormatter;
    private readonly bool _includeReservedTopics;

    /// <summary>Initializes an atomic client builder that skips reserved utility topics.</summary>
    /// <param name="baseNamespace">The base namespace for the generated clients (each client lands in <c>{baseNamespace}.{ClientName}</c>).</param>
    public AtomicClientSdkBuilder(string baseNamespace)
        : this(baseNamespace, new TopicMethodName(), false)
    { }

    /// <summary>Initializes an atomic client builder.</summary>
    /// <param name="baseNamespace">The base namespace for the generated clients.</param>
    /// <param name="clientNameFormatter">Derives each topic's client name from its topic id (defaults to <see cref="TopicMethodName"/>).</param>
    /// <param name="includeReservedTopics">When false (the default), reserved Benzene utility topics (spec/health/mesh/…) are skipped so only domain topics get atomic clients.</param>
    public AtomicClientSdkBuilder(string baseNamespace, IMethodName clientNameFormatter, bool includeReservedTopics)
    {
        _baseNamespace = baseNamespace;
        _clientNameFormatter = clientNameFormatter;
        _includeReservedTopics = includeReservedTopics;
    }

    /// <inheritdoc />
    public ICodeFile[] BuildCodeFiles(EventServiceDocument eventServiceDocument)
    {
        return eventServiceDocument.Requests
            .Where(request => _includeReservedTopics || !request.Reserved)
            .SelectMany(request => BuildForTopic(eventServiceDocument, request))
            .ToArray();
    }

    private ICodeFile[] BuildForTopic(EventServiceDocument document, RequestResponse request)
    {
        var clientName = _clientNameFormatter.Create(request.Topic, request.Request);

        var filtered = new EventServiceDocument(
            document.Info,
            document.Tags,
            new[] { request },
            Array.Empty<Event>(),
            new OpenApiComponents { Schemas = ReachableSchemas(document, request) })
        {
            MessageEndpoint = document.MessageEndpoint,
            Transports = document.Transports,
        };

        // Each atomic client is fully self-contained in its own namespace ({baseNamespace}.{clientName}),
        // so its files go under a per-client folder. This keeps a DTO shared by two topics (generated
        // once per client, each in that client's namespace) from colliding on a flat filename, and lets
        // a consumer drop a single client folder in for the one topic it calls.
        return new MessageClientSdkBuilder(clientName, _baseNamespace)
            .BuildCodeFiles(filtered)
            .Select(file => new CodeFile($"{clientName}/{file.Name}", file.Lines) as ICodeFile)
            .ToArray();
    }

    // Collects only the component schemas reachable from this one topic's request/response, so the
    // atomic client emits (and hashes) just that topic's DTOs rather than the whole service catalogue.
    private static IDictionary<string, OpenApiSchema> ReachableSchemas(EventServiceDocument document, RequestResponse request)
    {
        var catalogue = document.Components.Schemas;
        var reached = new HashSet<string>();

        void Walk(OpenApiSchema? schema)
        {
            if (schema == null)
            {
                return;
            }

            var referenceId = schema.Reference?.Id;
            // reached.Add short-circuits already-visited components, so reference cycles terminate.
            if (referenceId != null && catalogue.ContainsKey(referenceId) && reached.Add(referenceId))
            {
                Walk(catalogue[referenceId]);
            }

            Walk(schema.Items);
            Walk(schema.AdditionalProperties);
            foreach (var property in schema.Properties.Values)
            {
                Walk(property);
            }
            foreach (var composed in schema.AllOf.Concat(schema.AnyOf).Concat(schema.OneOf))
            {
                Walk(composed);
            }
        }

        Walk(request.Request);
        Walk(request.Response);

        return catalogue
            .Where(entry => reached.Contains(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value);
    }
}
