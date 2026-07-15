using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Results;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;
using JsonSerializer = Benzene.Core.MessageHandlers.Serialization.JsonSerializer;

namespace Benzene.Kafka.Core.Kafka;

public class KafkaBenzeneMessageClient : IBenzeneMessageClient
{
    // Shared across every SendMessageAsync call rather than constructed per call: JsonSerializer
    // wraps a JsonSerializerOptions instance, and System.Text.Json caches resolved converters/type
    // metadata per JsonSerializerOptions instance - a fresh one per send would silently defeat that
    // cache on every single outbound message. System.Text.Json's serializer is documented thread-safe
    // once its options are no longer being mutated, so one shared instance is safe here.
    private static readonly ISerializer SharedSerializer = new JsonSerializer();

    private readonly ILogger<KafkaBenzeneMessageClient> _logger;
    private readonly IServiceResolver _serviceResolver;
    private readonly IMiddlewarePipeline<KafkaSendMessageContext> _middlewarePipeline;

    public KafkaBenzeneMessageClient(IProducer<string, string> producer, ILogger<KafkaBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _logger = logger;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<KafkaSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseKafkaClient(producer)
            .Build();
    }

    public KafkaBenzeneMessageClient(IMiddlewarePipeline<KafkaSendMessageContext> middlewarePipeline, ILogger<KafkaBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _logger = logger;
        _middlewarePipeline = middlewarePipeline;
    }

    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {   var converter = new KafkaContextConverter<TRequest>(SharedSerializer);
            var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));

            await _middlewarePipeline.HandleAsync(context, _serviceResolver);

            var response = context.Response;
            
            if (response.Status == PersistenceStatus.Persisted)
            {
                return BenzeneResult.Accepted<TResponse>();
            }

            return BenzeneResult.UnexpectedError<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message {receiverTopic} failed", request.Topic);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    public void Dispose()
    {
        // Method intentionally left empty.
    }
}