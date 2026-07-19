using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Void = Benzene.Abstractions.Results.Void;
using JsonSerializer = Benzene.Core.MessageHandlers.Serialization.JsonSerializer;

namespace Benzene.RabbitMq.RabbitMqSendMessage;

/// <summary>
/// An <see cref="IBenzeneMessageClient"/> that publishes outbound messages to RabbitMQ, so business
/// logic depends only on <c>IBenzeneMessageSender</c>/<c>IBenzeneMessageClient</c> and stays
/// transport-agnostic. Mirrors <c>KafkaBenzeneMessageClient</c>: a message is converted to a
/// <see cref="RabbitMqSendMessageContext"/> and run through a one-middleware publish pipeline.
/// </summary>
public class RabbitMqBenzeneMessageClient : IBenzeneMessageClient
{
    // Shared across every send: JsonSerializer wraps a JsonSerializerOptions whose resolved
    // converter/type metadata is cached per instance, so a fresh one per send would defeat that cache.
    private static readonly ISerializer SharedSerializer = new JsonSerializer();

    private readonly ILogger<RabbitMqBenzeneMessageClient> _logger;
    private readonly IServiceResolver _serviceResolver;
    private readonly IMiddlewarePipeline<RabbitMqSendMessageContext> _middlewarePipeline;
    private readonly string _exchange;
    private readonly string _topicHeaderKey;

    /// <summary>Initializes a new instance publishing on the given channel to the given exchange.</summary>
    /// <param name="channel">The RabbitMQ channel to publish on.</param>
    /// <param name="logger">Logs a publish failure.</param>
    /// <param name="serviceResolver">The resolver the publish pipeline runs in.</param>
    /// <param name="exchange">The exchange to publish to (empty string for the default exchange).</param>
    /// <param name="mandatory">Whether an unroutable message is returned rather than dropped.</param>
    /// <param name="topicHeaderKey">The message-property header the topic is written to (defaults to <see cref="RabbitMqConstants.DefaultTopicHeader"/>).</param>
    public RabbitMqBenzeneMessageClient(IChannel channel, ILogger<RabbitMqBenzeneMessageClient> logger,
        IServiceResolver serviceResolver, string exchange = "", bool mandatory = false,
        string topicHeaderKey = RabbitMqConstants.DefaultTopicHeader)
    {
        _serviceResolver = serviceResolver;
        _logger = logger;
        _exchange = exchange;
        _topicHeaderKey = topicHeaderKey;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<RabbitMqSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseRabbitMqClient(channel, mandatory)
            .Build();
    }

    /// <summary>Initializes a new instance from an already-built publish pipeline (for testing).</summary>
    /// <param name="middlewarePipeline">The publish pipeline to run each message through.</param>
    /// <param name="logger">Logs a publish failure.</param>
    /// <param name="serviceResolver">The resolver the publish pipeline runs in.</param>
    /// <param name="exchange">The exchange to publish to (empty string for the default exchange).</param>
    /// <param name="topicHeaderKey">The message-property header the topic is written to (defaults to <see cref="RabbitMqConstants.DefaultTopicHeader"/>).</param>
    public RabbitMqBenzeneMessageClient(IMiddlewarePipeline<RabbitMqSendMessageContext> middlewarePipeline,
        ILogger<RabbitMqBenzeneMessageClient> logger, IServiceResolver serviceResolver, string exchange = "",
        string topicHeaderKey = RabbitMqConstants.DefaultTopicHeader)
    {
        _serviceResolver = serviceResolver;
        _logger = logger;
        _exchange = exchange;
        _topicHeaderKey = topicHeaderKey;
        _middlewarePipeline = middlewarePipeline;
    }

    /// <inheritdoc />
    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {
            var converter = new RabbitMqContextConverter<TRequest>(SharedSerializer, _exchange, _topicHeaderKey);
            var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));

            await _middlewarePipeline.HandleAsync(context, _serviceResolver);

            return context.Published
                ? BenzeneResult.Accepted<TResponse>()
                : BenzeneResult.UnexpectedError<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message {receiverTopic} failed", request.Topic);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Method intentionally left empty - the channel/connection lifetime is owned by the caller.
    }
}
