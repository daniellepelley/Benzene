using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Mesh.Wire;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Test.Mesh.Wire;

/// <summary>
/// The issue feed's emitter side (spec §4.1): the classification precedence table, the normative
/// fingerprint recipe, and the <c>UseMeshIssues</c> middleware — failures (result-status and
/// propagating-exception) produce occurrences, successes produce nothing, drain cancellation is
/// never an issue, the exemplar trace id rides in from the enclosing <c>UseMeshTrace</c>, and a
/// handler-converted exception's TYPE flows through the scoped <see cref="MessageErrorState"/>
/// into both the occurrence and the push-plane TraceEvent.
/// </summary>
public class MeshIssuesTest
{
    private sealed class CapturingIssueExporter : IMeshIssueExporter
    {
        public readonly List<MeshIssueOccurrence> Occurrences = new();
        public void Export(MeshIssueOccurrence occurrence) => Occurrences.Add(occurrence);
    }

    private sealed class CapturingTraceExporter : IMeshTraceExporter
    {
        public readonly List<MeshTraceEvent> Events = new();
        public void Export(MeshTraceEvent traceEvent) => Events.Add(traceEvent);
    }

    [Message("orders:throw")]
    public class ThrowingHandler : IMessageHandler<Void, Void>
    {
        public Task<IBenzeneResult<Void>> HandleAsync(Void request)
            => throw new InvalidOperationException("boom");
    }

    [Message("orders:notfound")]
    public class NotFoundHandler : IMessageHandler<Void, Void>
    {
        public Task<IBenzeneResult<Void>> HandleAsync(Void request)
            => Task.FromResult(BenzeneResult.NotFound<Void>("no such order"));
    }

    [Message("orders:ok")]
    public class OkHandler : IMessageHandler<Void, Void>
    {
        public Task<IBenzeneResult<Void>> HandleAsync(Void request)
            => Task.FromResult(BenzeneResult.Ok(new Void()));
    }

    private static async Task<(CapturingIssueExporter Issues, CapturingTraceExporter Traces, IBenzeneMessageResponse Response)>
        RunAsync(string topic)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage()
            .AddMessageHandlers(new[] { typeof(ThrowingHandler), typeof(NotFoundHandler), typeof(OkHandler) }));

        var issues = new CapturingIssueExporter();
        var traces = new CapturingTraceExporter();
        var info = new MeshServiceInfo("orders");
        var statusReader = new BenzeneMessageMeshStatusReader();

        var container = new MicrosoftBenzeneServiceContainer(services);
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        // The pinned ordering: trace outermost, issues immediately inside it — the issue's exemplar
        // trace id is the enclosing invocation's MeshSpan.
        pipeline
            .UseMeshTrace(info, traces, statusReader)
            .UseMeshIssues(info, issues, statusReader)
            .UseMessageHandlers(new[] { typeof(ThrowingHandler), typeof(NotFoundHandler), typeof(OkHandler) });

        var app = new BenzeneMessageApplication(pipeline.Build());
        var response = await app.HandleAsync(new BenzeneMessageRequest
        {
            Topic = topic,
            Headers = new Dictionary<string, string>(),
            Body = "{}"
        }, new MicrosoftServiceResolverFactory(services));

        return (issues, traces, response);
    }

    [Fact]
    public async Task Success_ProducesNoOccurrence()
    {
        var (issues, _, response) = await RunAsync("orders:ok");

        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
        Assert.Empty(issues.Occurrences);
    }

    [Fact]
    public async Task FailureResult_ProducesAnOccurrence_WithTheExemplarTraceId()
    {
        var (issues, traces, _) = await RunAsync("orders:notfound");

        var occurrence = Assert.Single(issues.Occurrences);
        Assert.Equal("orders", occurrence.Service);
        Assert.Equal("orders:notfound", occurrence.Topic);
        Assert.Equal(BenzeneResultStatus.NotFound, occurrence.Status);
        Assert.Null(occurrence.ExceptionType); // an explicit result, nothing thrown
        // Wired inside UseMeshTrace, the occurrence carries the same trace the TraceEvent records.
        Assert.Equal(Assert.Single(traces.Events).TraceId, occurrence.TraceId);
    }

    [Fact]
    public async Task ConvertedException_CarriesTheType_IntoTheOccurrenceAndTheTraceEvent()
    {
        var (issues, traces, _) = await RunAsync("orders:throw");

        // The handler layer converts the throw into a service-unavailable result; the scoped
        // MessageErrorState carries the TYPE to both mesh feeds (type only, never the message).
        var occurrence = Assert.Single(issues.Occurrences);
        Assert.Equal(BenzeneResultStatus.ServiceUnavailable, occurrence.Status);
        Assert.Equal("System.InvalidOperationException", occurrence.ExceptionType);
        Assert.Equal("System.InvalidOperationException", Assert.Single(traces.Events).ExceptionType);
    }

    [Fact]
    public async Task NullExporter_IsAPassThrough()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage().AddMessageHandlers(new[] { typeof(OkHandler) }));
        var container = new MicrosoftBenzeneServiceContainer(services);
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        pipeline.UseMeshIssues(new MeshServiceInfo("orders"), exporter: null)
            .UseMessageHandlers(new[] { typeof(OkHandler) });

        var app = new BenzeneMessageApplication(pipeline.Build());
        var response = await app.HandleAsync(new BenzeneMessageRequest
        {
            Topic = "orders:ok", Headers = new Dictionary<string, string>(), Body = "{}"
        }, new MicrosoftServiceResolverFactory(services));

        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
    }

    [Fact]
    public void Classification_FollowsThePrecedenceTable()
    {
        // 1. Validation statuses win even when an exception was thrown.
        Assert.Equal(MeshIssueClassification.Validation, MeshIssueClassification.Classify("bad-request", "System.Text.Json.JsonException"));
        Assert.Equal(MeshIssueClassification.Validation, MeshIssueClassification.Classify("validation-error", null));
        // 2. A thrown-and-converted failure classifies by its throw, not its mapped status.
        Assert.Equal(MeshIssueClassification.Exception, MeshIssueClassification.Classify("service-unavailable", "System.NullReferenceException"));
        // 3. Wiring/config drift, including the empty-status wiring gap; unauthorized is config, not dependency.
        Assert.Equal(MeshIssueClassification.ConfigWiring, MeshIssueClassification.Classify("not-found", null));
        Assert.Equal(MeshIssueClassification.ConfigWiring, MeshIssueClassification.Classify("unauthorized", null));
        Assert.Equal(MeshIssueClassification.ConfigWiring, MeshIssueClassification.Classify("", null));
        // 4. Dependency only when nothing was thrown.
        Assert.Equal(MeshIssueClassification.Dependency, MeshIssueClassification.Classify("service-unavailable", null));
        Assert.Equal(MeshIssueClassification.Dependency, MeshIssueClassification.Classify("timeout", null));
        // 5-6. The wire's unclassified-failure status is exception-shaped; anything else is honest.
        Assert.Equal(MeshIssueClassification.Exception, MeshIssueClassification.Classify("unexpected-error", null));
        Assert.Equal(MeshIssueClassification.Unclassified, MeshIssueClassification.Classify("conflict", null));
    }

    [Fact]
    public void Fingerprint_FollowsTheNormativeRecipe()
    {
        // Deterministic, 32 lowercase hex chars, exceptionType preferred as the discriminator,
        // transport never part of the identity.
        var a = MeshIssueFingerprint.Compute("orders", "order:create", "v2", "exception", "System.TimeoutException", "service-unavailable");
        var b = MeshIssueFingerprint.Compute("orders", "order:create", "v2", "exception", "System.TimeoutException", "timeout");
        var c = MeshIssueFingerprint.Compute("orders", "order:create", "v2", "dependency", null, "timeout");

        Assert.Matches("^[0-9a-f]{32}$", a);
        Assert.Equal(a, b); // same discriminator (the exception type) → same identity
        Assert.NotEqual(a, c); // different classification/discriminator → different identity
        // The spec's worked derivation: sha256("orders|order:create|v2|exception|System.TimeoutException")[..16] hex.
        Assert.Equal(a, MeshIssueFingerprint.Compute("orders", "order:create", "v2", "exception", null, "System.TimeoutException"));
    }
}
