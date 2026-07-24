using System.Threading;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Messages;
using Benzene.Mesh.Collector;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Mesh.Test;

/// <summary>
/// A second <c>AddMessageHandlers(Type[])</c> call (e.g. an inner <c>UseBenzeneMessage</c> pipeline's own
/// <c>UseMessageHandlers</c>, which scans a different type set than the app's outer scan) must be composed
/// into handler discovery, not silently dropped. Before the fix the single composite
/// <c>IMessageHandlersFinder</c> was registered via <c>TryAdd</c> and captured only the FIRST call's
/// reflection finder, so the second call's handlers were constructable but never routable — the
/// "No handler found for topic 'mesh:query:fleet'" the composite fleet endpoint hit on AwsMesh.
/// </summary>
public class MultipleAddMessageHandlersCompositionTest
{
    [Message("test:first")]
    public class FirstMessageHandler : IMessageHandler<FirstRequest, FirstResponse>
    {
        public Task<IBenzeneResult<FirstResponse>> HandleAsync(FirstRequest request)
            => Task.FromResult(BenzeneResult.Ok(new FirstResponse()));
    }

    public class FirstRequest { }
    public class FirstResponse { }

    private sealed class FakeReadModel : IMeshFleetReadModel
    {
        public Task<FleetView> FleetAsync(CancellationToken cancellationToken = default) => Task.FromResult(new FleetView());
        public Task<ServiceView?> ServiceAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult<ServiceView?>(null);
        public Task<TopicSummary?> TopicAsync(string id, string? version, CancellationToken cancellationToken = default) => Task.FromResult<TopicSummary?>(null);
        public Task<TraceView?> TraceAsync(string traceId, CancellationToken cancellationToken = default) => Task.FromResult<TraceView?>(null);
        public Task<CorrelationView?> CorrelationAsync(string correlationId, CancellationToken cancellationToken = default) => Task.FromResult<CorrelationView?>(null);
    }

    [Fact]
    public void SecondAddMessageHandlersCall_HandlersAreStillDiscoverable()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddBenzene();
        // First scan: an unrelated type set (the app's own handlers) — mirrors the mesh Lambda's
        // ConfigureServices AddMessageHandlers(typeof(Startup).Assembly).
        container.AddMessageHandlers(new[] { typeof(FirstMessageHandler) });
        // Second scan: the collector query handlers (the inner fleet pipeline's set).
        container.AddMessageHandlers(MeshCollectorHandlers.Queries);
        container.AddSingleton<IMeshFleetReadModel>(new FakeReadModel());

        using var resolver = container.CreateServiceResolverFactory().CreateScope();
        var lookup = resolver.GetService<IMessageHandlerDefinitionLookUp>();

        // Both sets' topics resolve — the second call composed in, not dropped.
        Assert.NotNull(lookup.FindHandler(new Topic("test:first")));
        Assert.NotNull(lookup.FindHandler(new Topic("mesh:query:fleet")));
        Assert.NotNull(lookup.FindHandler(new Topic("mesh:query:trace")));
    }
}
