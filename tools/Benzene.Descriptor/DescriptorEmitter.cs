using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Mesh.Wire;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;

namespace Benzene.Descriptor;

/// <summary>
/// Emits a cloud-agnostic deployment descriptor from a built, non-running Benzene service. The logical
/// contract (consumes, produces, schemas, outbound transport kinds) is derived from host-neutral
/// registration; a host adapter (AWS Lambda today) additionally supplies the inbound transport-name
/// list. No deploy, no socket.
/// </summary>
internal static class DescriptorEmitter
{
    public static string Emit(EmitOptions options)
    {
        var fullPath = Path.GetFullPath(options.AssemblyPath);
        var alc = new ServiceLoadContext(fullPath);
        var assembly = alc.LoadFromAssemblyPath(fullPath);

        var startUpType = FindStartUpType(assembly);
        var adapter = options.Host is { } forced
            ? HostAdapters.All.FirstOrDefault(a => a.Name == forced)
              ?? throw new InvalidOperationException($"Unknown host adapter '{forced}'. Known: {string.Join(", ", HostAdapters.All.Select(a => a.Name))}.")
            : HostAdapters.Select(assembly);
        var build = adapter.Build(startUpType);

        var specJson = BuildSpec(build.Resolver);
        var meshJson = BuildMesh(assembly, options);
        var outboundTransports = OutboundRouteInspector.TransportsByTopic(build.Resolver);

        return Distil(specJson, meshJson, outboundTransports, build, options);
    }

    private static Type FindStartUpType(Assembly assembly)
    {
        var candidates = assembly.GetTypes()
            .Where(t => typeof(BenzeneStartUp).IsAssignableFrom(t) && !t.IsAbstract
                        && t.GetConstructor(Type.EmptyTypes) != null)
            .ToArray();

        return candidates.Length switch
        {
            0 => throw new InvalidOperationException(
                $"No public parameterless BenzeneStartUp found in {assembly.GetName().Name}."),
            1 => candidates[0],
            _ => throw new InvalidOperationException(
                $"Multiple BenzeneStartUp types found ({string.Join(", ", candidates.Select(c => c.Name))}); " +
                "specify one with --startup <fullTypeName>.")
        };
    }

    // Runs SpecBuilder directly against the built container — no host round-trip, no I/O. For the
    // neutral adapter this yields everything except the inbound transports (empty); for a host adapter
    // it also carries transports and validation-enriched schemas.
    private static string BuildSpec(Benzene.Abstractions.DI.IServiceResolver resolver)
    {
        var stdout = Console.Out;
        Console.SetOut(Console.Error);
        try
        {
            return new SpecBuilder().CreateSpec(resolver, new SpecRequest("benzene", "json"));
        }
        finally
        {
            Console.SetOut(stdout);
        }
    }

    private static string BuildMesh(Assembly assembly, EmitOptions options)
    {
        var definitions = new ReflectionMessageHandlersFinder(assembly).FindDefinitions();
        var descriptor = MeshDescriptorFactory.Create(
            new DefinitionsLookUp(definitions),
            new MeshServiceInfo(options.ServiceName, serviceVersion: options.ServiceVersion,
                instanceId: options.ServiceName,
                placement: new MeshPlacement { Cloud = options.Cloud, Region = options.Region }));
        return MeshJson.Serialize(descriptor);
    }

    private static string Distil(string specJson, string meshJson,
        IReadOnlyDictionary<string, string> outboundTransports, HostBuild build, EmitOptions options)
    {
        var spec = JsonNode.Parse(specJson)?.AsObject();
        var mesh = JsonNode.Parse(meshJson)?.AsObject();
        var schemas = spec?["components"]?["schemas"]?.AsObject();

        JsonNode? Resolve(JsonNode? node)
        {
            if (node is JsonObject o && o["$ref"]?.GetValue<string>() is { } r)
                return schemas?[r.Split('/').Last()]?.DeepClone();
            return node?.DeepClone();
        }

        var transports = new JsonArray();
        if (spec?["transports"] is JsonArray specTransports)
            foreach (var t in specTransports) transports.Add(JsonValue.Create(t?.GetValue<string>()));

        var consumes = new JsonArray();
        if (spec?["requests"] is JsonArray requests)
            foreach (var r in requests.OfType<JsonObject>())
            {
                if (r["reserved"]?.GetValue<bool>() == true) continue;
                var http = new JsonArray();
                if (r["httpMappings"] is JsonArray maps)
                    foreach (var m in maps.OfType<JsonObject>())
                        http.Add(new JsonObject { ["method"] = m["method"]?.DeepClone(), ["path"] = (m["path"] ?? m["url"])?.DeepClone() });
                consumes.Add(new JsonObject
                {
                    ["topic"] = r["topic"]?.DeepClone(),
                    ["http"] = http,
                    ["requestSchema"] = Resolve(r["request"]),
                    ["responseSchema"] = Resolve(r["response"]),
                });
            }

        var produces = new JsonArray();
        if (spec?["events"] is JsonArray events)
            foreach (var e in events.OfType<JsonObject>())
            {
                var topic = e["topic"]?.GetValue<string>();
                produces.Add(new JsonObject
                {
                    ["topic"] = topic,
                    // Cloud-agnostic transport kind; destination (env-var binding) is intentionally
                    // omitted pending the outbound-routing redesign. See work/deployment-descriptor-design.md.
                    ["transportKind"] = topic != null && outboundTransports.TryGetValue(topic, out var tk) ? tk : "unknown",
                    ["messageSchema"] = Resolve(e["message"]),
                });
            }

        var doc = new JsonObject
        {
            ["descriptorVersion"] = "0.1",
            ["service"] = options.ServiceName,
            ["serviceVersion"] = options.ServiceVersion,
            ["placement"] = new JsonObject { ["cloud"] = options.Cloud, ["region"] = options.Region },
            ["host"] = build.HostName,
            ["transportsResolved"] = build.TransportsResolved,
            ["transports"] = transports,
            ["consumes"] = consumes,
            ["produces"] = produces,
            ["descriptorHash"] = mesh?["descriptorHash"]?.DeepClone(),
        };
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }

    private sealed class DefinitionsLookUp : IMessageHandlerDefinitionLookUp
    {
        private readonly IMessageHandlerDefinition[] _definitions;
        public DefinitionsLookUp(IMessageHandlerDefinition[] definitions) => _definitions = definitions;
        public IMessageHandlerDefinition? FindHandler(ITopic topic)
            => _definitions.FirstOrDefault(x => x.Topic.Id == topic.Id && x.Topic.Version == topic.Version);
        public IMessageHandlerDefinition[] GetAllHandlers() => _definitions;
    }
}
