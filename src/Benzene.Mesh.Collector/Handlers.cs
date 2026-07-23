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

    /// <summary>
    /// The read-side <c>mesh:query:*</c> handlers only — no ingest (<c>mesh:register</c>/
    /// <c>heartbeat</c>/<c>traces</c>). Register these when the fleet read model is composed from an
    /// external backend (a <c>Benzene.Mesh.Fleet.*</c> adapter) rather than the in-memory push
    /// collector: there is no ring to ingest into, only an <see cref="IMeshFleetReadModel"/> to query.
    /// These depend solely on <see cref="IMeshFleetReadModel"/>, not <see cref="MeshCollectorStore"/>.
    /// </summary>
    public static readonly Type[] Queries =
    {
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

/// <summary>The whole known fleet in one read model. Depends on <see cref="IMeshFleetReadModel"/> so
/// the fleet's data source is swappable (in-memory collector, or a backend-composed reader).</summary>
[Message("mesh:query:fleet")]
public class FleetQueryMessageHandler : IMessageHandler<FleetQuery, FleetView>
{
    private readonly IMeshFleetReadModel _readModel;

    public FleetQueryMessageHandler(IMeshFleetReadModel readModel) => _readModel = readModel;

    public async Task<IBenzeneResult<FleetView>> HandleAsync(FleetQuery request)
    {
        return BenzeneResult.Ok(await _readModel.FleetAsync());
    }
}

/// <summary>One service: fleet row + descriptor + per-instance heartbeat state.</summary>
[Message("mesh:query:service")]
public class ServiceQueryMessageHandler : IMessageHandler<ServiceQuery, ServiceView>
{
    private readonly IMeshFleetReadModel _readModel;

    public ServiceQueryMessageHandler(IMeshFleetReadModel readModel) => _readModel = readModel;

    public async Task<IBenzeneResult<ServiceView>> HandleAsync(ServiceQuery request)
    {
        if (string.IsNullOrEmpty(request.Service))
        {
            return BenzeneResult.BadRequest<ServiceView>("service is required");
        }
        var view = await _readModel.ServiceAsync(request.Service!);
        return view == null
            ? BenzeneResult.NotFound<ServiceView>($"unknown service {request.Service}")
            : BenzeneResult.Ok(view);
    }
}

/// <summary>One topic's catalog row, consumers derived from trace parentage at query time.</summary>
[Message("mesh:query:topic")]
public class TopicQueryMessageHandler : IMessageHandler<TopicQuery, TopicSummary>
{
    private readonly IMeshFleetReadModel _readModel;

    public TopicQueryMessageHandler(IMeshFleetReadModel readModel) => _readModel = readModel;

    public async Task<IBenzeneResult<TopicSummary>> HandleAsync(TopicQuery request)
    {
        if (string.IsNullOrEmpty(request.Topic))
        {
            return BenzeneResult.BadRequest<TopicSummary>("topic is required");
        }
        var view = await _readModel.TopicAsync(request.Topic!, request.Version);
        return view == null
            ? BenzeneResult.NotFound<TopicSummary>($"unknown topic {request.Topic}")
            : BenzeneResult.Ok(view);
    }
}

/// <summary>One flow's events in start order → the waterfall.</summary>
[Message("mesh:query:trace")]
public class TraceQueryMessageHandler : IMessageHandler<TraceQuery, TraceView>
{
    private readonly IMeshFleetReadModel _readModel;

    public TraceQueryMessageHandler(IMeshFleetReadModel readModel) => _readModel = readModel;

    public async Task<IBenzeneResult<TraceView>> HandleAsync(TraceQuery request)
    {
        if (string.IsNullOrEmpty(request.TraceId))
        {
            return BenzeneResult.BadRequest<TraceView>("traceId is required");
        }
        var view = await _readModel.TraceAsync(request.TraceId!);
        return view == null
            ? BenzeneResult.NotFound<TraceView>($"unknown trace {request.TraceId}")
            : BenzeneResult.Ok(view);
    }
}

/// <summary>Every flow that carried a business correlation id, grouped by trace. Complements
/// <c>mesh:query:trace</c> for cross-service failure triage from a business identifier (a ticket/log
/// correlation id) rather than a trace id.</summary>
[Message("mesh:query:correlation")]
public class CorrelationQueryMessageHandler : IMessageHandler<CorrelationQuery, CorrelationView>
{
    private readonly IMeshFleetReadModel _readModel;

    public CorrelationQueryMessageHandler(IMeshFleetReadModel readModel) => _readModel = readModel;

    public async Task<IBenzeneResult<CorrelationView>> HandleAsync(CorrelationQuery request)
    {
        if (string.IsNullOrEmpty(request.CorrelationId))
        {
            return BenzeneResult.BadRequest<CorrelationView>("correlationId is required");
        }
        var view = await _readModel.CorrelationAsync(request.CorrelationId!);
        return view == null
            ? BenzeneResult.NotFound<CorrelationView>($"no flows for correlation {request.CorrelationId}")
            : BenzeneResult.Ok(view);
    }
}
