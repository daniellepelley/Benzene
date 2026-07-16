using System.Text.Json;
using Benzene.Conformance.Test.Handlers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Mesh.Wire;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Conformance.Test;

/// <summary>
/// Runs docs/specification/conformance/mesh-trace-cases.json through the real BenzeneMessage
/// pipeline with the mesh trace middleware installed outermost (mesh.md §3): the traceparent
/// join/reject rules, and the invocation→semantic-status mapping including the
/// <c>conformance:panic</c> canonical handler's exception rule.
/// </summary>
public class MeshTraceConformanceTest
{
    public class TraceFixture
    {
        public List<TraceparentCase> Traceparent { get; set; } = new();
        public List<InvocationCase> Invocations { get; set; } = new();
    }

    public class TraceparentCase
    {
        public string Name { get; set; } = string.Empty;
        public string Header { get; set; } = string.Empty;
        public bool Valid { get; set; }
        public string? TraceId { get; set; }
        public string? ParentSpanId { get; set; }
    }

    public class InvocationCase
    {
        public string Name { get; set; } = string.Empty;
        public EnvelopeConformanceTest.EnvelopeRequest Request { get; set; } = new();
        public JsonElement ExpectedEvent { get; set; }
    }

    private static readonly Lazy<TraceFixture> Fixture = new(() =>
        ConformanceFixtures.Load<TraceFixture>("mesh-trace-cases.json"));

    public static IEnumerable<object[]> TraceparentCaseNames() =>
        Fixture.Value.Traceparent.Select(x => new object[] { x.Name });

    public static IEnumerable<object[]> InvocationCaseNames() =>
        Fixture.Value.Invocations.Select(x => new object[] { x.Name });

    private class CaptureExporter : IMeshTraceExporter
    {
        public List<MeshTraceEvent> Events { get; } = new();

        public void Export(MeshTraceEvent traceEvent) => Events.Add(traceEvent);
    }

    private static async Task<MeshTraceEvent> RunTracedAsync(BenzeneMessageRequest request)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzene().AddBenzeneMessage();

        var exporter = new CaptureExporter();
        var pipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        pipelineBuilder.UseMeshTrace(
            new MeshServiceInfo("conformance"),
            exporter,
            new BenzeneMessageMeshStatusReader());
        pipelineBuilder.UseMessageHandlers(
            typeof(GreetConformanceHandler), typeof(StatusConformanceHandler), typeof(PanicConformanceHandler));
        var pipeline = pipelineBuilder.Build();

        var application = new BenzeneMessageApplication(pipeline);
        await application.HandleAsync(request, container.CreateServiceResolverFactory());

        var traceEvent = Assert.Single(exporter.Events);
        return traceEvent;
    }

    [Theory]
    [MemberData(nameof(TraceparentCaseNames))]
    public async Task TraceparentCase_JoinsOrStartsFresh(string caseName)
    {
        var traceparentCase = Fixture.Value.Traceparent.Single(x => x.Name == caseName);
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(traceparentCase.Header))
        {
            headers["traceparent"] = traceparentCase.Header;
        }

        var traceEvent = await RunTracedAsync(new BenzeneMessageRequest
        {
            Topic = "conformance:greet",
            Headers = headers,
            Body = "{\"name\":\"world\"}"
        });

        if (traceparentCase.Valid)
        {
            Assert.Equal(traceparentCase.TraceId, traceEvent.TraceId);
            Assert.Equal(traceparentCase.ParentSpanId, traceEvent.ParentSpanId);
            Assert.NotEqual(traceparentCase.ParentSpanId, traceEvent.SpanId);
            return;
        }

        Assert.Null(traceEvent.ParentSpanId);
        Assert.Equal(32, traceEvent.TraceId.Length);
        Assert.Matches("^[0-9a-f]+$", traceEvent.TraceId);
        var segments = traceparentCase.Header.Split('-');
        if (segments.Length > 1)
        {
            Assert.NotEqual(segments[1], traceEvent.TraceId);
        }
    }

    [Theory]
    [MemberData(nameof(InvocationCaseNames))]
    public async Task InvocationCase_ProducesTheExpectedEvent(string caseName)
    {
        var invocationCase = Fixture.Value.Invocations.Single(x => x.Name == caseName);

        var traceEvent = await RunTracedAsync(new BenzeneMessageRequest
        {
            Topic = invocationCase.Request.Topic,
            Headers = invocationCase.Request.Headers,
            Body = invocationCase.Request.Body
        });

        using var actual = JsonDocument.Parse(MeshJson.Serialize(traceEvent));
        var mismatch = ConformanceFixtures.FindSubsetMismatch(invocationCase.ExpectedEvent, actual.RootElement);
        Assert.True(mismatch == null, $"{caseName}: event mismatch at {mismatch}");

        Assert.Equal(16, traceEvent.SpanId.Length);
        Assert.True(traceEvent.DurationMs >= 0);
        Assert.NotEqual(default, traceEvent.StartedAt);
    }
}
