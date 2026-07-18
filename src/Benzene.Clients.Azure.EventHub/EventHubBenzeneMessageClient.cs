using System;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Producer;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Clients.Common;
using Benzene.Core.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.EventHub;

/// <summary>
/// A Benzene message client that sends outgoing messages to an Azure Event Hub via a caller-supplied
/// <see cref="EventHubProducerClient"/>.
/// </summary>
public class EventHubBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly ILogger<EventHubBenzeneMessageClient> _logger;
    private readonly IMiddlewarePipeline<EventHubSendMessageContext> _middlewarePipeline;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubBenzeneMessageClient"/> class, building a
    /// default single-middleware pipeline around the given producer client.
    /// </summary>
    /// <param name="producerClient">The Event Hubs producer client to send with.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public EventHubBenzeneMessageClient(EventHubProducerClient producerClient, ILogger<EventHubBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _logger = logger;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<EventHubSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseEventHubClient(producerClient)
            .Build();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHubBenzeneMessageClient"/> class with a
    /// pre-built middleware pipeline.
    /// </summary>
    /// <param name="middlewarePipeline">The built middleware pipeline to send through.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public EventHubBenzeneMessageClient(IMiddlewarePipeline<EventHubSendMessageContext> middlewarePipeline, ILogger<EventHubBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _middlewarePipeline = middlewarePipeline;
        _logger = logger;
    }

    /// <summary>
    /// Sends the request as an event to the configured Event Hub.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="request">The client request to send.</param>
    /// <returns>
    /// A task that resolves to an accepted result if the send succeeded, or a service-unavailable result
    /// if the send threw. Event Hubs has no request/response semantics beyond a send acknowledgement.
    /// </returns>
    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {
            var converter = new EventHubContextConverter<TRequest>(new JsonSerializer());
            var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));

            await _middlewarePipeline.HandleAsync(context, _serviceResolver);

            return BenzeneResult.Accepted<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message {receiverTopic} to Event Hubs failed", request.Topic);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    /// <summary>
    /// Disposes the client. No-op; the caller owns the <see cref="EventHubProducerClient"/>'s lifetime.
    /// </summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
