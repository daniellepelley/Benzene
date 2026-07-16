using System.Text.Json;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Mesh.Collector;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Conformance.Test;

/// <summary>
/// Runs docs/specification/conformance/mesh-collector-cases.json: each case's steps run in order
/// against one fresh <see cref="MeshCollectorStore"/>-backed pipeline - ingest validation,
/// health/hash-mismatch surfacing, consumer derivation from trace parentage, re-registration
/// semantics, and the degradation matrix of mesh.md §4-§6. Expected bodies use the mesh matching
/// rule: subset objects, arrays exact-length with per-element subset, and an expected empty array
/// matches an absent-or-empty actual one.
/// </summary>
public class MeshCollectorConformanceTest
{
    public class CollectorFixture
    {
        public List<CollectorCase> Cases { get; set; } = new();
    }

    public class CollectorCase
    {
        public string Name { get; set; } = string.Empty;
        public List<CollectorStep> Steps { get; set; } = new();
    }

    public class CollectorStep
    {
        public EnvelopeConformanceTest.EnvelopeRequest Request { get; set; } = new();
        public StepExpectation Expected { get; set; } = new();
    }

    public class StepExpectation
    {
        public string StatusCode { get; set; } = string.Empty;
        public JsonElement? Body { get; set; }
    }

    private static readonly Lazy<CollectorFixture> Fixture = new(() =>
        ConformanceFixtures.Load<CollectorFixture>("mesh-collector-cases.json"));

    public static IEnumerable<object[]> CaseNames() =>
        Fixture.Value.Cases.Select(x => new object[] { x.Name });

    [Theory]
    [MemberData(nameof(CaseNames))]
    public async Task CollectorCase_StepsProduceTheExpectedResponses(string caseName)
    {
        var collectorCase = Fixture.Value.Cases.Single(x => x.Name == caseName);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new MeshCollectorStore());

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzene().AddBenzeneMessage();

        var pipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        pipelineBuilder.UseMessageHandlers(MeshCollectorHandlers.All);
        var pipeline = pipelineBuilder.Build();
        var application = new BenzeneMessageApplication(pipeline);
        var resolverFactory = container.CreateServiceResolverFactory();

        for (var i = 0; i < collectorCase.Steps.Count; i++)
        {
            var step = collectorCase.Steps[i];
            var response = await application.HandleAsync(new BenzeneMessageRequest
            {
                Topic = step.Request.Topic,
                Headers = step.Request.Headers,
                Body = step.Request.Body
            }, resolverFactory);

            Assert.True(step.Expected.StatusCode == response.StatusCode,
                $"step {i} ({step.Request.Topic}): statusCode '{response.StatusCode}', expected '{step.Expected.StatusCode}' (body: {response.Body})");

            if (step.Expected.Body is not { } expectedBody)
            {
                continue;
            }
            Assert.False(string.IsNullOrEmpty(response.Body), $"step {i}: expected a body but none was written");
            using var actualBody = JsonDocument.Parse(response.Body);
            var mismatch = FindMeshSubsetMismatch(expectedBody, actualBody.RootElement, $"step {i} $");
            Assert.True(mismatch == null, $"{caseName}: body mismatch at {mismatch}");
        }
    }

    /// <summary>
    /// The mesh fixtures' matching rule (conformance/README.md "Mesh fixture formats"): like
    /// <see cref="ConformanceFixtures.FindSubsetMismatch"/>, plus an expected empty array matches
    /// an actual that is empty or absent (writers may omit empty collections).
    /// </summary>
    private static string? FindMeshSubsetMismatch(JsonElement expected, JsonElement actual, string path)
    {
        if (expected.ValueKind == JsonValueKind.Object)
        {
            if (actual.ValueKind != JsonValueKind.Object)
            {
                return $"{path}: expected an object but found {actual.ValueKind}";
            }
            foreach (var property in expected.EnumerateObject())
            {
                if (!actual.TryGetProperty(property.Name, out var actualValue))
                {
                    if (property.Value.ValueKind == JsonValueKind.Array && property.Value.GetArrayLength() == 0)
                    {
                        continue; // expected [] matches an omitted empty collection
                    }
                    return $"{path}.{property.Name}: missing";
                }
                var mismatch = FindMeshSubsetMismatch(property.Value, actualValue, $"{path}.{property.Name}");
                if (mismatch != null)
                {
                    return mismatch;
                }
            }
            return null;
        }

        if (expected.ValueKind == JsonValueKind.Array)
        {
            if (actual.ValueKind != JsonValueKind.Array)
            {
                return $"{path}: expected an array but found {actual.ValueKind}";
            }
            var expectedItems = expected.EnumerateArray().ToArray();
            var actualItems = actual.EnumerateArray().ToArray();
            if (expectedItems.Length != actualItems.Length)
            {
                return $"{path}: expected {expectedItems.Length} items but found {actualItems.Length}";
            }
            for (var i = 0; i < expectedItems.Length; i++)
            {
                var mismatch = FindMeshSubsetMismatch(expectedItems[i], actualItems[i], $"{path}[{i}]");
                if (mismatch != null)
                {
                    return mismatch;
                }
            }
            return null;
        }

        return ConformanceFixtures.FindSubsetMismatch(expected, actual, path);
    }
}
