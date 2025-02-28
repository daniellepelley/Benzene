using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Results;
using Confluent.Kafka;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Kafka.Core.Kafka;

public class KafkaBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneLogger _logger;
    private readonly IServiceResolver _serviceResolver;
    private readonly IMiddlewarePipeline<KafkaSendMessageContext> _middlewarePipeline;

    public KafkaBenzeneMessageClient(IProducer<string, string> producer, IBenzeneLogger logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _logger = logger;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<KafkaSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseKafkaClient(producer)
            .Build();
    }

    public KafkaBenzeneMessageClient(IMiddlewarePipeline<KafkaSendMessageContext> middlewarePipeline, IBenzeneLogger logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _logger = logger;
        _middlewarePipeline = middlewarePipeline;
    }

    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {   var converter = new KafkaContextConverter<TRequest>(new JsonSerializer());
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