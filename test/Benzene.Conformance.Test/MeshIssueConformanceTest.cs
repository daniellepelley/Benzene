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
/// Runs docs/specification/conformance/mesh-issue-cases.json (mesh.md §4.1 — the optional issue
/// feed, required only for collectors claiming it): <c>mesh:issues</c> ingest validation, the
/// empty-batch liveness assertion, fingerprint delta-merge arithmetic observable via
/// <c>mesh:query:fleet</c>, invalid-entry skipping, and the failure-gated feed-absence derivation
/// (<c>missingFeeds: ["issues"]</c>). Same harness and matching rule as
/// <see cref="MeshCollectorConformanceTest"/>.
/// </summary>
public class MeshIssueConformanceTest
{
    private static readonly Lazy<MeshCollectorConformanceTest.CollectorFixture> Fixture = new(() =>
        ConformanceFixtures.Load<MeshCollectorConformanceTest.CollectorFixture>("mesh-issue-cases.json"));

    public static IEnumerable<object[]> CaseNames() =>
        Fixture.Value.Cases.Select(x => new object[] { x.Name });

    [Theory]
    [MemberData(nameof(CaseNames))]
    public async Task IssueCase_StepsProduceTheExpectedResponses(string caseName)
    {
        var issueCase = Fixture.Value.Cases.Single(x => x.Name == caseName);

        var services = new ServiceCollection();
        services.AddLogging();
        var collectorStore = new MeshCollectorStore();
        services.AddSingleton(collectorStore);
        services.AddSingleton<IMeshFleetReadModel>(collectorStore);

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzene().AddBenzeneMessage();

        var pipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        pipelineBuilder.UseMessageHandlers(MeshCollectorHandlers.All);
        var pipeline = pipelineBuilder.Build();
        var application = new BenzeneMessageApplication(pipeline);
        var resolverFactory = container.CreateServiceResolverFactory();

        for (var i = 0; i < issueCase.Steps.Count; i++)
        {
            var step = issueCase.Steps[i];
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
            var mismatch = MeshCollectorConformanceTest.FindMeshSubsetMismatch(expectedBody, actualBody.RootElement, $"step {i} $");
            Assert.True(mismatch == null, $"{caseName}: body mismatch at {mismatch}");
        }
    }
}
