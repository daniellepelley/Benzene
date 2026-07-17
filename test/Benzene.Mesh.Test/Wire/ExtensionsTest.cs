using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Mesh.Wire;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Mesh.Wire;

public class ExtensionsTest
{
    private static (MiddlewarePipelineBuilder<BenzeneMessageContext> Pipeline, ServiceCollection Services) NewPipeline()
    {
        var services = new ServiceCollection();
        services.UsingBenzene(x => x.AddBenzene().AddBenzeneMessage().AddContextItems());
        var pipeline = new MiddlewarePipelineBuilder<BenzeneMessageContext>(new MicrosoftBenzeneServiceContainer(services));
        return (pipeline, services);
    }

    private static async Task<IBenzeneMessageResponse> SendAsync(
        MiddlewarePipelineBuilder<BenzeneMessageContext> pipeline, ServiceCollection services,
        string topic, IDictionary<string, string>? headers = null)
    {
        var app = new BenzeneMessageApplication(pipeline.Build());
        var request = new BenzeneMessageRequest
        {
            Topic = topic,
            Headers = headers ?? new Dictionary<string, string>(),
            Body = string.Empty
        };
        return await app.HandleAsync(request, new MicrosoftServiceResolverFactory(services));
    }

    [Fact]
    public async Task UseMeshDescriptor_MatchingTopic_ShortCircuitsWithTheDescriptor()
    {
        var (pipeline, services) = NewPipeline();
        var descriptor = new MeshServiceDescriptor { Service = "orders-service" };
        pipeline.UseMeshDescriptor(descriptor);

        var response = await SendAsync(pipeline, services, MeshTopics.Descriptor);

        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
        Assert.Contains("orders-service", response.Body);
    }

    [Fact]
    public async Task UseMeshDescriptor_AliasTopic_ShortCircuitsWithTheDescriptor()
    {
        var (pipeline, services) = NewPipeline();
        var descriptor = new MeshServiceDescriptor { Service = "orders-service" };
        pipeline.UseMeshDescriptor(descriptor, "orders.mesh");

        var response = await SendAsync(pipeline, services, "orders.mesh");

        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
        Assert.Contains("orders-service", response.Body);
    }

    [Fact]
    public async Task UseMeshDescriptor_NonMatchingTopic_FallsThroughToNextMiddleware()
    {
        var (pipeline, services) = NewPipeline();
        var descriptor = new MeshServiceDescriptor { Service = "orders-service" };
        var nextRan = false;
        pipeline
            .UseMeshDescriptor(descriptor)
            .Use(_ => new FuncWrapperMiddleware<BenzeneMessageContext>("Terminal", (_, next) =>
            {
                nextRan = true;
                return next();
            }));

        await SendAsync(pipeline, services, "some-other-topic");

        Assert.True(nextRan);
    }

    [Fact]
    public async Task UseMeshTrace_NullExporter_IsAPassThrough()
    {
        var (pipeline, services) = NewPipeline();
        var nextRan = false;
        pipeline
            .UseMeshTrace(new MeshServiceInfo("orders-service"), null)
            .Use(_ => new FuncWrapperMiddleware<BenzeneMessageContext>("Terminal", (_, next) =>
            {
                nextRan = true;
                return next();
            }));

        await SendAsync(pipeline, services, "some-topic");

        Assert.True(nextRan);
    }

    [Fact]
    public async Task UseMeshTrace_SuccessfulInvocation_ExportsATraceEventWithTopicAndStatus()
    {
        var (pipeline, services) = NewPipeline();
        MeshTraceEvent? exported = null;
        var mockExporter = new Mock<IMeshTraceExporter>();
        mockExporter.Setup(x => x.Export(It.IsAny<MeshTraceEvent>())).Callback<MeshTraceEvent>(e => exported = e);

        pipeline
            .UseMeshTrace(new MeshServiceInfo("orders-service"), mockExporter.Object, new BenzeneMessageMeshStatusReader())
            .UseMeshDescriptor(new MeshServiceDescriptor { Service = "orders-service" });

        await SendAsync(pipeline, services, MeshTopics.Descriptor);

        Assert.NotNull(exported);
        Assert.Equal(MeshTopics.Descriptor, exported!.Topic);
        Assert.Equal("orders-service", exported.Service);
        Assert.Equal(BenzeneResultStatus.Ok, exported.Status);
        Assert.True(exported.DurationMs >= 0);
        Assert.NotEmpty(exported.TraceId);
        Assert.NotEmpty(exported.SpanId);
    }

    [Fact]
    public async Task UseMeshTrace_NoIncomingTraceparent_StartsANewTrace()
    {
        var (pipeline, services) = NewPipeline();
        MeshTraceEvent? exported = null;
        var mockExporter = new Mock<IMeshTraceExporter>();
        mockExporter.Setup(x => x.Export(It.IsAny<MeshTraceEvent>())).Callback<MeshTraceEvent>(e => exported = e);

        pipeline.UseMeshTrace(new MeshServiceInfo("orders-service"), mockExporter.Object);

        await SendAsync(pipeline, services, "some-topic");

        Assert.NotNull(exported);
        Assert.Equal(32, exported!.TraceId.Length);
        Assert.Null(exported.ParentSpanId);
    }

    [Fact]
    public async Task UseMeshTrace_ValidIncomingTraceparent_JoinsTheExistingTrace()
    {
        var (pipeline, services) = NewPipeline();
        MeshTraceEvent? exported = null;
        var mockExporter = new Mock<IMeshTraceExporter>();
        mockExporter.Setup(x => x.Export(It.IsAny<MeshTraceEvent>())).Callback<MeshTraceEvent>(e => exported = e);

        pipeline.UseMeshTrace(new MeshServiceInfo("orders-service"), mockExporter.Object);

        var traceId = new string('a', 32);
        var parentSpanId = new string('b', 16);
        var headers = new Dictionary<string, string> { ["traceparent"] = $"00-{traceId}-{parentSpanId}-01" };

        await SendAsync(pipeline, services, "some-topic", headers);

        Assert.NotNull(exported);
        Assert.Equal(traceId, exported!.TraceId);
        Assert.Equal(parentSpanId, exported.ParentSpanId);
    }

    [Fact]
    public async Task UseMeshTrace_MalformedTraceparent_StartsANewTraceInstead()
    {
        var (pipeline, services) = NewPipeline();
        MeshTraceEvent? exported = null;
        var mockExporter = new Mock<IMeshTraceExporter>();
        mockExporter.Setup(x => x.Export(It.IsAny<MeshTraceEvent>())).Callback<MeshTraceEvent>(e => exported = e);

        pipeline.UseMeshTrace(new MeshServiceInfo("orders-service"), mockExporter.Object);

        var headers = new Dictionary<string, string> { ["traceparent"] = "not-a-valid-traceparent" };

        await SendAsync(pipeline, services, "some-topic", headers);

        Assert.NotNull(exported);
        Assert.Equal(32, exported!.TraceId.Length);
        Assert.Null(exported.ParentSpanId);
    }

    [Fact]
    public async Task UseMeshTrace_ExporterThrows_DoesNotAffectTheResponse()
    {
        var (pipeline, services) = NewPipeline();
        var mockExporter = new Mock<IMeshTraceExporter>();
        mockExporter.Setup(x => x.Export(It.IsAny<MeshTraceEvent>())).Throws(new System.InvalidOperationException("boom"));

        pipeline
            .UseMeshTrace(new MeshServiceInfo("orders-service"), mockExporter.Object)
            .UseMeshDescriptor(new MeshServiceDescriptor { Service = "orders-service" });

        var response = await SendAsync(pipeline, services, MeshTopics.Descriptor);

        Assert.Equal(BenzeneResultStatus.Ok, response.StatusCode);
        mockExporter.Verify(x => x.Export(It.IsAny<MeshTraceEvent>()), Times.Once);
    }

    [Fact]
    public async Task UseMeshTrace_NoStatusReader_RecordsAnEmptyStatus()
    {
        var (pipeline, services) = NewPipeline();
        MeshTraceEvent? exported = null;
        var mockExporter = new Mock<IMeshTraceExporter>();
        mockExporter.Setup(x => x.Export(It.IsAny<MeshTraceEvent>())).Callback<MeshTraceEvent>(e => exported = e);

        pipeline
            .UseMeshTrace(new MeshServiceInfo("orders-service"), mockExporter.Object)
            .UseMeshDescriptor(new MeshServiceDescriptor { Service = "orders-service" });

        await SendAsync(pipeline, services, MeshTopics.Descriptor);

        Assert.NotNull(exported);
        Assert.Equal(string.Empty, exported!.Status);
    }

    [Fact]
    public async Task UseMeshTrace_CorrelationIdHeader_IsCapturedOnTheTraceEvent()
    {
        var (pipeline, services) = NewPipeline();
        MeshTraceEvent? exported = null;
        var mockExporter = new Mock<IMeshTraceExporter>();
        mockExporter.Setup(x => x.Export(It.IsAny<MeshTraceEvent>())).Callback<MeshTraceEvent>(e => exported = e);

        pipeline.UseMeshTrace(new MeshServiceInfo("orders-service"), mockExporter.Object);

        var headers = new Dictionary<string, string> { ["x-correlation-id"] = "corr-123" };

        await SendAsync(pipeline, services, "some-topic", headers);

        Assert.NotNull(exported);
        Assert.Equal("corr-123", exported!.CorrelationId);
    }

    [Fact]
    public async Task UseMeshTrace_DuringNext_MeshSpanCurrentIsSetThenRestoredAfterwards()
    {
        var (pipeline, services) = NewPipeline();
        var before = MeshSpan.Current;
        string? traceIdDuringNext = null;

        pipeline
            .UseMeshTrace(new MeshServiceInfo("orders-service"), new Mock<IMeshTraceExporter>().Object)
            .Use(_ => new FuncWrapperMiddleware<BenzeneMessageContext>("Capture", (_, next) =>
            {
                traceIdDuringNext = MeshSpan.Current?.TraceId;
                return next();
            }));

        await SendAsync(pipeline, services, "some-topic");

        Assert.NotNull(traceIdDuringNext);
        Assert.Equal(32, traceIdDuringNext!.Length);
        Assert.Same(before, MeshSpan.Current);
    }
}
