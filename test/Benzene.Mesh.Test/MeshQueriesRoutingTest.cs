using System.Threading;
using System.Threading.Tasks;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Core.Middleware;
using Benzene.Mesh.Collector;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// The read-only <c>MeshCollectorHandlers.Queries</c> list (the one the backend-composed fleet planes —
/// X-Ray/Tempo/Jaeger — serve over <c>/benzene/invoke</c>) must route <c>mesh:query:*</c> to an
/// <see cref="IMeshFleetReadModel"/> with no <see cref="MeshCollectorStore"/> present. This pins that a
/// query-only wiring answers the Fleet UI's <c>mesh:query:fleet</c> poll (the AwsMesh regression where the
/// UI reported "collector unreachable (HTTP 404)").
/// </summary>
public class MeshQueriesRoutingTest
{
    private sealed class FakeReadModel : IMeshFleetReadModel
    {
        public Task<FleetView> FleetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new FleetView());
        public Task<ServiceView?> ServiceAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult<ServiceView?>(null);
        public Task<TopicSummary?> TopicAsync(string id, string? version, CancellationToken cancellationToken = default)
            => Task.FromResult<TopicSummary?>(null);
        public Task<TraceView?> TraceAsync(string traceId, CancellationToken cancellationToken = default)
            => Task.FromResult<TraceView?>(null);
        public Task<CorrelationView?> CorrelationAsync(string correlationId, CancellationToken cancellationToken = default)
            => Task.FromResult<CorrelationView?>(null);
    }

    private static async Task<BenzeneMessageResponse> DispatchAsync(string topic, string body)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMeshFleetReadModel>(new FakeReadModel());

        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzene().AddBenzeneMessage();

        var pipelineBuilder = new MiddlewarePipelineBuilder<BenzeneMessageContext>(container);
        pipelineBuilder.UseMessageHandlers(MeshCollectorHandlers.Queries);
        var pipeline = pipelineBuilder.Build();
        var application = new BenzeneMessageApplication(pipeline);
        var resolverFactory = container.CreateServiceResolverFactory();

        return (BenzeneMessageResponse)await application.HandleAsync(
            new BenzeneMessageRequest { Topic = topic, Body = body }, resolverFactory);
    }

    [Fact]
    public async Task Queries_RouteFleetQuery_ToTheReadModel_WithNoStore()
    {
        var response = await DispatchAsync("mesh:query:fleet", "{}");
        Assert.Equal("ok", response.StatusCode);
    }

    [Fact]
    public async Task Queries_RouteTraceQuery_ToTheReadModel()
    {
        // The fake read model returns null for an unknown trace → the handler answers NotFound (not the
        // "no handler for topic" NotFound the UI would get from an unrouted topic — this proves it routed).
        var response = await DispatchAsync("mesh:query:trace", """{ "traceId": "abc" }""");
        Assert.Equal("not-found", response.StatusCode);
    }
}
