using System.Text.Json;
using System.Text.Json.Nodes;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.TestHelpers;
using Benzene.Examples.AwsMesh.Payments;
using Benzene.Examples.AwsMesh.Payments.Handlers;
using Benzene.Mesh.Wire;
using Benzene.Schema.OpenApi;
using Benzene.Testing;
using Benzene.Tools.Aws;

// ---------------------------------------------------------------------------------------------------
// Deployment-descriptor spike.
//
// Constructs the REAL payments-api service (examples/AwsMesh/Payments) in-process — running its
// StartUp.ConfigureServices + Configure exactly as AwsLambdaHost<Startup> does on cold start — WITHOUT
// deploying it, opening a socket, or touching AWS. Then it asks the constructed pipeline for the two
// descriptors it already knows how to emit (the `spec` and `mesh` topics), and distils a neutral
// service.json from them. Nothing here contacts the network.
// ---------------------------------------------------------------------------------------------------

var outDir = Path.Combine(AppContext.BaseDirectory, "spike-output");
// Prefer writing next to the source when run via `dotnet run` from the repo.
var srcOut = Path.Combine(Directory.GetCurrentDirectory(), "output");
if (Directory.Exists(Path.GetDirectoryName(srcOut)!) || Directory.Exists(srcOut))
{
    outDir = srcOut;
}
Directory.CreateDirectory(outDir);

Console.WriteLine("== Constructing payments-api in-process (no deploy, no network) ==");

// 1. Build the service exactly as the Lambda host does — ConfigureServices + Configure — but never
//    call the run/listen step. This is the whole "non-running" trick.
var entryPoint = BenzeneTestHost.Create<Startup>().BuildAwsLambdaHost();
using var host = new AwsLambdaBenzeneTestHost(entryPoint);

// 2. The `spec` topic: the derived Cloud-Service spec (requests it consumes, events it produces,
//    transports it is wired over, HTTP routes) — served from startup registrations, no I/O.
var specResponse = await host.SendBenzeneMessageAsync(
    MessageBuilder.Create("spec", new SpecRequest("benzene", "json")));
var specJson = specResponse.Body ?? "";
File.WriteAllText(Path.Combine(outDir, "spec.json"), Pretty(specJson));
Console.WriteLine($"\n== spec (benzene) — {specJson.Length} bytes ==\n{Pretty(specJson)}");

// 3. The `mesh` topic: the ServiceDescriptor (mesh.md §2), built straight from the handler types via
//    reflection — the lightest path, needs no host at all (the CloudServiceDescriptorSource pattern).
Type[] handlerTypes = { typeof(GetPaymentsMessageHandler), typeof(CapturePaymentMessageHandler) };
var lookUp = new SpikeLookUp(new ReflectionMessageHandlersFinder(handlerTypes).FindDefinitions());
var descriptor = MeshDescriptorFactory.Create(
    lookUp,
    new MeshServiceInfo("payments", serviceVersion: "1.0.0", instanceId: "payments",
        placement: new MeshPlacement { Cloud = "aws", Region = "eu-west-1" }));
var meshJson = MeshJson.Serialize(descriptor);
File.WriteAllText(Path.Combine(outDir, "mesh.json"), Pretty(meshJson));
Console.WriteLine($"\n== mesh (ServiceDescriptor) ==\n{Pretty(meshJson)}");

// 4. Distil a neutral deployment descriptor from the real spec doc + mesh descriptor.
var serviceJson = BuildDeploymentDescriptor(specJson, meshJson);
File.WriteAllText(Path.Combine(outDir, "service.json"), serviceJson);
Console.WriteLine($"\n== distilled service.json ==\n{serviceJson}");
Console.WriteLine($"\nWrote spec.json, mesh.json, service.json to {outDir}");

// ---------------------------------------------------------------------------------------------------

static string Pretty(string json)
{
    if (string.IsNullOrWhiteSpace(json)) return json;
    try
    {
        return JsonSerializer.Serialize(JsonNode.Parse(json), new JsonSerializerOptions { WriteIndented = true });
    }
    catch { return json; }
}

// Projects the neutral "infra needs" view from what the service genuinely reports at build time.
static string BuildDeploymentDescriptor(string specJson, string meshJson)
{
    var spec = JsonNode.Parse(specJson)?.AsObject();
    var mesh = JsonNode.Parse(meshJson)?.AsObject();
    var schemas = spec?["components"]?["schemas"]?.AsObject();

    // Resolve a { "$ref": "#/components/schemas/X" } node to the actual (inlined) schema.
    JsonNode? Resolve(JsonNode? node)
    {
        if (node is JsonObject o && o["$ref"]?.GetValue<string>() is { } r)
        {
            var name = r.Split('/').Last();
            return schemas?[name]?.DeepClone();
        }
        return node?.DeepClone();
    }

    var transports = new JsonArray();
    if (spec?["transports"] is JsonArray specTransports)
        foreach (var t in specTransports) transports.Add(JsonValue.Create(t?.GetValue<string>()));

    // Consumed topics (skip Benzene's own reserved topics like `spec`/`healthcheck`/`mesh`) with their
    // HTTP routes and inlined request/response schemas — the domain ingress surface.
    var consumes = new JsonArray();
    if (spec?["requests"] is JsonArray requests)
    {
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
    }

    // Produced/egress topics from the spec's declared events (response-event declarations).
    var produces = new JsonArray();
    if (spec?["events"] is JsonArray events)
        foreach (var e in events.OfType<JsonObject>())
            produces.Add(new JsonObject
            {
                ["topic"] = e["topic"]?.DeepClone(),
                ["messageSchema"] = Resolve(e["message"]),
                // transportKind + destinationRef (env-var) are NOT in the spec today — surfacing them
                // generically needs the small outbound-routing read-model proposed in the design doc.
                ["transportKind"] = "TODO: needs outbound-routing accessor",
                ["destinationRef"] = "TODO: env-var name, needs outbound-routing accessor",
            });

    var doc = new JsonObject
    {
        ["descriptorVersion"] = "0.1-spike",
        ["service"] = mesh?["service"]?.DeepClone() ?? spec?["info"]?["title"]?.DeepClone(),
        ["serviceVersion"] = mesh?["serviceVersion"]?.DeepClone(),
        ["placement"] = mesh?["placement"]?.DeepClone(),
        ["transports"] = transports,
        ["consumes"] = consumes,
        ["produces"] = produces,
        ["descriptorHash"] = mesh?["descriptorHash"]?.DeepClone(),
    };
    return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
}

// A minimal in-memory handler lookup (mirrors the private one in the conformance tests).
internal sealed class SpikeLookUp : IMessageHandlerDefinitionLookUp
{
    private readonly IMessageHandlerDefinition[] _definitions;
    public SpikeLookUp(IMessageHandlerDefinition[] definitions) => _definitions = definitions;

    public IMessageHandlerDefinition? FindHandler(ITopic topic)
        => _definitions.FirstOrDefault(x => x.Topic.Id == topic.Id && x.Topic.Version == topic.Version);

    public IMessageHandlerDefinition[] GetAllHandlers() => _definitions;
}
