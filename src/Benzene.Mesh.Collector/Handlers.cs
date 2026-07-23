using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Mesh.Wire;
using Benzene.Results;

namespace Benzene.Mesh.Collector;

/// <summary>
/// The collector's message handlers (docs/specification/mesh.md §4) - the collector is an
/// ordinary Benzene service, dogfooded like the aggregator's own handlers. Register them with
/// <c>UseMessageHandlers(MeshCollectorHandlers.All)</c> and a singleton
/// <see cref="MeshCollectorStore"/> in the container.
/// </summary>
public static class MeshCollectorHandlers
{
    public static readonly Type[] All =
    {
        typeof(RegisterMessageHandler),
        typeof(HeartbeatMessageHandler),
        typeof(TracesMessageHandler),
        typeof(FleetQueryMessageHandler),
        typeof(ServiceQueryMessageHandler),
        typeof(TopicQueryMessageHandler),
        typeof(TraceQueryMessageHandler),
        typeof(CorrelationQueryMessageHandler)
    };
}

/// <summary>Ingests a service's descriptor (spec §4): re-registration replaces provider edges wholesale.</summary>
[Message(MeshTopics.Register)]
public class RegisterMessageHandler : IMessageHandler<MeshServiceDescriptor, Ack>
{
    private readonly MeshCollectorStore _store;

    public RegisterMessageHandler(MeshCollectorStore store) => _store = store;

    public Task<IBenzeneResult<Ack>> HandleAsync(MeshServiceDescriptor request)
    {
        if (string.IsNullOrEmpty(request.Service))
        {
            return Task.FromResult(BenzeneResult.BadRequest<Ack>("service is required"));
        }
        _store.Register(request);
        return Task.FromResult(BenzeneResult.Ok(new Ack { Accepted = 1 }));
    }
}

/// <summary>Ingests one instance's health report (spec §5).</summary>
[Message(MeshTopics.Heartbeat)]
public class HeartbeatMessageHandler : IMessageHandler<MeshHeartbeat, Ack>
{
    private readonly MeshCollectorStore _store;

    public HeartbeatMessageHandler(MeshCollectorStore store) => _store = store;

    public Task<IBenzeneResult<Ack>> HandleAsync(MeshHeartbeat request)
    {
        if (string.IsNullOrEmpty(request.Service))
        {
            return Task.FromResult(BenzeneResult.BadRequest<Ack>("service is required"));
        }
        _store.Heartbeat(request);
        return Task.FromResult(BenzeneResult.Ok(new Ack { Accepted = 1 }));
    }
}

/// <summary>Ingests a trace batch (spec §4): a batch of any size, including empty, is accepted.</summary>
[Message(MeshTopics.Traces)]
public class TracesMessageHandler : IMessageHandler<MeshTraceBatch, Ack>
{
    private readonly MeshCollectorStore _store;

    public TracesMessageHandler(MeshCollectorStore store) => _store = store;

    public Task<IBenzeneResult<Ack>> HandleAsync(MeshTraceBatch request)
    {
        return Task.FromResult(BenzeneResult.Ok(new Ack { Accepted = _store.AddEvents(request.Events) }));
    }
}

/// <summary>The whole known fleet in one read model.</summary>
[Message("mesh:query:fleet")]
public class FleetQueryMessageHandler : IMessageHandler<FleetQuery, FleetView>
{
    private readonly MeshCollectorStore _store;

    public FleetQueryMessageHandler(MeshCollectorStore store) => _store = store;

    public Task<IBenzeneResult<FleetView>> HandleAsync(FleetQuery request)
    {
        return Task.FromResult(BenzeneResult.Ok(_store.Fleet()));
    }
}

/// <summary>One service: fleet row + descriptor + per-instance heartbeat state.</summary>
[Message("mesh:query:service")]
public class ServiceQueryMessageHandler : IMessageHandler<ServiceQuery, ServiceView>
{
    private readonly MeshCollectorStore _store;

    public ServiceQueryMessageHandler(MeshCollectorStore store) => _store = store;

    public Task<IBenzeneResult<ServiceView>> HandleAsync(ServiceQuery request)
    {
        if (string.IsNullOrEmpty(request.Service))
        {
            return Task.FromResult(BenzeneResult.BadRequest<ServiceView>("service is required"));
        }
        var view = _store.Service(request.Service!);
        return Task.FromResult(view == null
            ? BenzeneResult.NotFound<ServiceView>($"unknown service {request.Service}")
            : BenzeneResult.Ok(view));
    }
}

/// <summary>One topic's catalog row, consumers derived from trace parentage at query time.</summary>
[Message("mesh:query:topic")]
public class TopicQueryMessageHandler : IMessageHandler<TopicQuery, TopicSummary>
{
    private readonly MeshCollectorStore _store;

    public TopicQueryMessageHandler(MeshCollectorStore store) => _store = store;

    public Task<IBenzeneResult<TopicSummary>> HandleAsync(TopicQuery request)
    {
        if (string.IsNullOrEmpty(request.Topic))
        {
            return Task.FromResult(BenzeneResult.BadRequest<TopicSummary>("topic is required"));
        }
        var view = _store.Topic(request.Topic!, request.Version);
        return Task.FromResult(view == null
            ? BenzeneResult.NotFound<TopicSummary>($"unknown topic {request.Topic}")
            : BenzeneResult.Ok(view));
    }
}

/// <summary>One flow's events in start order, from the bounded ring window.</summary>
[Message("mesh:query:trace")]
public class TraceQueryMessageHandler : IMessageHandler<TraceQuery, TraceView>
{
    private readonly MeshCollectorStore _store;

    public TraceQueryMessageHandler(MeshCollectorStore store) => _store = store;

    public Task<IBenzeneResult<TraceView>> HandleAsync(TraceQuery request)
    {
        if (string.IsNullOrEmpty(request.TraceId))
        {
            return Task.FromResult(BenzeneResult.BadRequest<TraceView>("traceId is required"));
        }
        var view = _store.Trace(request.TraceId!);
        return Task.FromResult(view == null
            ? BenzeneResult.NotFound<TraceView>($"unknown trace {request.TraceId}")
            : BenzeneResult.Ok(view));
    }
}

/// <summary>Every flow that carried a business correlation id, grouped by trace, from the ring
/// window. Complements <c>mesh:query:trace</c> for cross-service failure triage from a business
/// identifier (a ticket/log correlation id) rather than a trace id.</summary>
[Message("mesh:query:correlation")]
public class CorrelationQueryMessageHandler : IMessageHandler<CorrelationQuery, CorrelationView>
{
    private readonly MeshCollectorStore _store;

    public CorrelationQueryMessageHandler(MeshCollectorStore store) => _store = store;

    public Task<IBenzeneResult<CorrelationView>> HandleAsync(CorrelationQuery request)
    {
        if (string.IsNullOrEmpty(request.CorrelationId))
        {
            return Task.FromResult(BenzeneResult.BadRequest<CorrelationView>("correlationId is required"));
        }
        var view = _store.Correlation(request.CorrelationId!);
        return Task.FromResult(view == null
            ? BenzeneResult.NotFound<CorrelationView>($"no flows for correlation {request.CorrelationId}")
            : BenzeneResult.Ok(view));
    }
}
