using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Aws.Lambda.Core;
using Benzene.Aws.Lambda.Core.AwsEventStream;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.TestHelpers;
using Benzene.Core.Middleware;
using Benzene.Mesh.Wire;
using Benzene.Microsoft.Dependencies;
using Benzene.Schema.OpenApi;
using Benzene.Testing;
using Benzene.Tools.Aws;
using Microsoft.Extensions.DependencyInjection;

namespace Benzene.Descriptor;

/// <summary>
/// Constructs a built Benzene AWS Lambda service in-process — running its StartUp registration exactly
/// as the Lambda host does on cold start, but never starting the run/listen step — then reads the
/// `spec` descriptor it already computes and distils a neutral deployment descriptor. No network I/O.
/// </summary>
internal static class DescriptorEmitter
{
    public static string Emit(EmitOptions options)
    {
        var fullPath = Path.GetFullPath(options.AssemblyPath);
        var alc = new ServiceLoadContext(fullPath);
        var assembly = alc.LoadFromAssemblyPath(fullPath);

        var startUpType = FindStartUpType(assembly);
        var specJson = BuildSpec(startUpType);
        var meshJson = BuildMesh(assembly, options);

        return Distil(specJson, meshJson, options);
    }

    // Locate the service's BenzeneStartUp (shared base type ⇒ assignable across the ALC boundary).
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

    // Replicates AwsLambdaHost<TStartUp>'s constructor (registration + pipeline build) without the
    // Lambda runtime, then asks the built pipeline for the `spec` document over an in-memory message.
    private static string BuildSpec(Type startUpType)
    {
        var startUp = (BenzeneStartUp)Activator.CreateInstance(startUpType)!;
        var configuration = startUp.GetConfiguration();

        var services = new ServiceCollection();
        services.AddLogging();
        var container = new MicrosoftBenzeneServiceContainer(services);
        var eventPipeline = new MiddlewarePipelineBuilder<AwsEventStreamContext>(container);

        startUp.ConfigureServices(services, configuration);
        startUp.Configure(new AwsLambdaApplicationBuilder(eventPipeline, container), configuration);

        var entryPoint = new AwsLambdaEntryPoint(eventPipeline.Build(), new MicrosoftServiceResolverFactory(services));
        using var host = new AwsLambdaBenzeneTestHost(entryPoint);

        // The service's own console logger writes to stdout while the pipeline runs; keep our stdout
        // (where the descriptor may be printed) clean by diverting that to stderr for the send.
        var stdout = Console.Out;
        Console.SetOut(Console.Error);
        try
        {
            var response = host.SendBenzeneMessageAsync(
                MessageBuilder.Create("spec", new SpecRequest("benzene", "json"))).GetAwaiter().GetResult();
            return response.Body ?? "";
        }
        finally
        {
            Console.SetOut(stdout);
        }
    }

    // The mesh ServiceDescriptor builds straight from the handler types (no host) — used here only for
    // the content-addressed descriptorHash and the placement/version identity fields.
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

    // Projects the neutral deployment descriptor from the real spec + mesh. NOTE: the produces[] entries
    // deliberately carry topic + payload schema only — the transport kind + destination env-var per
    // egress are paused pending the outbound-routing design (see work/deployment-descriptor-design.md).
    private static string Distil(string specJson, string meshJson, EmitOptions options)
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
                produces.Add(new JsonObject
                {
                    ["topic"] = e["topic"]?.DeepClone(),
                    ["messageSchema"] = Resolve(e["message"]),
                    // transportKind + destinationRef intentionally omitted — paused pending outbound design.
                });

        var doc = new JsonObject
        {
            ["descriptorVersion"] = "0.1",
            ["service"] = options.ServiceName,
            ["serviceVersion"] = options.ServiceVersion,
            ["placement"] = new JsonObject { ["cloud"] = options.Cloud, ["region"] = options.Region },
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
