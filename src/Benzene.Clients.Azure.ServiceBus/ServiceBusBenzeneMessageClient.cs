using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Clients.Common;
using Benzene.Core.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.ServiceBus;

/// <summary>
/// A Benzene message client that sends outgoing messages to an Azure Service Bus queue or topic via a
/// caller-supplied <see cref="ServiceBusSender"/>.
/// </summary>
public class ServiceBusBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly ILogger<ServiceBusBenzeneMessageClient> _logger;
    private readonly IMiddlewarePipeline<ServiceBusSendMessageContext> _middlewarePipeline;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusBenzeneMessageClient"/> class, building a
    /// default single-middleware pipeline around the given Service Bus sender.
    /// </summary>
    /// <param name="sender">The Service Bus sender (bound to a queue or topic) to send with.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public ServiceBusBenzeneMessageClient(ServiceBusSender sender, ILogger<ServiceBusBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _logger = logger;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<ServiceBusSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseServiceBusClient(sender)
            .Build();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusBenzeneMessageClient"/> class with a
    /// pre-built middleware pipeline.
    /// </summary>
    /// <param name="middlewarePipeline">The built middleware pipeline to send through.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public ServiceBusBenzeneMessageClient(IMiddlewarePipeline<ServiceBusSendMessageContext> middlewarePipeline, ILogger<ServiceBusBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _middlewarePipeline = middlewarePipeline;
        _logger = logger;
    }

    /// <summary>
    /// Sends the request as a message to the configured Service Bus entity.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="request">The client request to send.</param>
    /// <returns>
    /// A task that resolves to an accepted result if the send succeeded, or a service-unavailable result
    /// if the send threw. Service Bus has no request/response semantics beyond a send acknowledgement.
    /// </returns>
    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {
            var converter = new ServiceBusContextConverter<TRequest>(new JsonSerializer());
            var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));

            await _middlewarePipeline.HandleAsync(context, _serviceResolver);

            return BenzeneResult.Accepted<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message {receiverTopic} to Service Bus failed", request.Topic);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    /// <summary>
    /// Disposes the client. No-op; the caller owns the <see cref="ServiceBusSender"/>'s lifetime.
    /// </summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
