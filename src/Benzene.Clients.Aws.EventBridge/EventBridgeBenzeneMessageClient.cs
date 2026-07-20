using System;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Core.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.EventBridge;

/// <summary>
/// A Benzene message client that publishes messages as EventBridge events via <c>PutEvents</c>:
/// topic → <c>detail-type</c>, message → <c>detail</c>, headers embedded per the
/// <c>_benzeneHeaders</c> convention (see <see cref="EventBridgeContextConverter{T}"/>). Publishing
/// is fire-and-forget — a successful publish maps to <c>Accepted</c>.
/// </summary>
public class EventBridgeBenzeneMessageClient : IBenzeneMessageClient
{
    // Reuse one serializer across sends: a fresh JsonSerializer per call defeats System.Text.Json's
    // per-options converter/metadata cache (matching the Kafka/RabbitMQ clients).
    private static readonly JsonSerializer SharedSerializer = new();
    private readonly ILogger<EventBridgeBenzeneMessageClient> _logger;
    private readonly string _source;
    private readonly string _eventBusName;
    private readonly IServiceResolver _serviceResolver;
    private readonly IMiddlewarePipeline<EventBridgeSendMessageContext> _middlewarePipeline;

    public EventBridgeBenzeneMessageClient(string source, IAmazonEventBridge amazonEventBridge,
        ILogger<EventBridgeBenzeneMessageClient> logger, IServiceResolver serviceResolver, string eventBusName = null)
    {
        _source = source;
        _eventBusName = eventBusName;
        _serviceResolver = serviceResolver;
        _logger = logger;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<EventBridgeSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseEventBridgeClient(amazonEventBridge)
            .Build();
    }

    public EventBridgeBenzeneMessageClient(string source, IMiddlewarePipeline<EventBridgeSendMessageContext> middlewarePipeline,
        ILogger<EventBridgeBenzeneMessageClient> logger, IServiceResolver serviceResolver, string eventBusName = null)
    {
        _source = source;
        _eventBusName = eventBusName;
        _middlewarePipeline = middlewarePipeline;
        _logger = logger;
        _serviceResolver = serviceResolver;
    }

    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {
            var converter = new EventBridgeContextConverter<TRequest>(_source, _eventBusName, SharedSerializer);
            var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));

            await _middlewarePipeline.HandleAsync(context, _serviceResolver);

            var result = EventBridgeResultMapper.Map<TResponse>(context.Response);

            _logger.LogInformation("Published event {receiverTopic} to EventBridge source {source} with status {status}",
                request.Topic, _source, result.Status);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Publishing event {receiverTopic} to EventBridge source {source} failed", request.Topic, _source);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
