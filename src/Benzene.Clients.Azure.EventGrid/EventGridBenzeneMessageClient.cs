using System;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Clients.Common;
using Benzene.Core.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.EventGrid;

/// <summary>
/// A Benzene message client that sends outgoing messages to Azure Event Grid, as CloudEvents 1.0, via a
/// caller-supplied <see cref="EventGridPublisherClient"/>.
/// </summary>
public class EventGridBenzeneMessageClient : IBenzeneMessageClient
{
    // Reuse one serializer across sends: a fresh JsonSerializer per call defeats System.Text.Json's
    // per-options converter/metadata cache (matching the Kafka/RabbitMQ clients).
    private static readonly JsonSerializer SharedSerializer = new();
    private readonly ILogger<EventGridBenzeneMessageClient> _logger;
    private readonly string _source;
    private readonly IMiddlewarePipeline<EventGridSendMessageContext> _middlewarePipeline;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridBenzeneMessageClient"/> class, building a
    /// default single-middleware pipeline around the given publisher client.
    /// </summary>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    /// <param name="publisherClient">The Event Grid publisher client to send with.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public EventGridBenzeneMessageClient(string source, EventGridPublisherClient publisherClient, ILogger<EventGridBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _source = source;
        _logger = logger;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<EventGridSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseEventGridClient(publisherClient)
            .Build();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventGridBenzeneMessageClient"/> class with a
    /// pre-built middleware pipeline.
    /// </summary>
    /// <param name="source">The CloudEvent <c>source</c> - identifies the context the event happened in.</param>
    /// <param name="middlewarePipeline">The built middleware pipeline to send through.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public EventGridBenzeneMessageClient(string source, IMiddlewarePipeline<EventGridSendMessageContext> middlewarePipeline, ILogger<EventGridBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _source = source;
        _middlewarePipeline = middlewarePipeline;
        _logger = logger;
    }

    /// <summary>
    /// Sends the request as a CloudEvent to the configured Event Grid topic.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="request">The client request to send.</param>
    /// <returns>
    /// A task that resolves to an accepted result if the send succeeded, or a service-unavailable result
    /// if the send threw. Event Grid has no request/response semantics beyond a send acknowledgement.
    /// </returns>
    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {
            var converter = new EventGridContextConverter<TRequest>(_source, SharedSerializer);
            var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));

            await _middlewarePipeline.HandleAsync(context, _serviceResolver);

            return BenzeneResult.Accepted<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message {receiverTopic} to Event Grid failed", request.Topic);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    /// <summary>
    /// Disposes the client. No-op; the caller owns the <see cref="EventGridPublisherClient"/>'s lifetime.
    /// </summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
