using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Clients.Common;
using Benzene.Core.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Azure.QueueStorage;

/// <summary>
/// A Benzene message client that sends outgoing messages to an Azure Storage queue, wrapped in a
/// <see cref="Core.Messages.BenzeneMessage.BenzeneMessageRequest"/> envelope, via a caller-supplied
/// <see cref="QueueClient"/>.
/// </summary>
public class QueueStorageBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly ILogger<QueueStorageBenzeneMessageClient> _logger;
    private readonly IMiddlewarePipeline<QueueStorageSendMessageContext> _middlewarePipeline;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageBenzeneMessageClient"/> class, building
    /// a default single-middleware pipeline around the given queue client.
    /// </summary>
    /// <param name="queueClient">The queue client to send with.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public QueueStorageBenzeneMessageClient(QueueClient queueClient, ILogger<QueueStorageBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _logger = logger;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<QueueStorageSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseQueueStorageClient(queueClient)
            .Build();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueStorageBenzeneMessageClient"/> class with a
    /// pre-built middleware pipeline.
    /// </summary>
    /// <param name="middlewarePipeline">The built middleware pipeline to send through.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public QueueStorageBenzeneMessageClient(IMiddlewarePipeline<QueueStorageSendMessageContext> middlewarePipeline, ILogger<QueueStorageBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _middlewarePipeline = middlewarePipeline;
        _logger = logger;
    }

    /// <summary>
    /// Sends the request as an enveloped message to the configured queue.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="request">The client request to send.</param>
    /// <returns>
    /// A task that resolves to an accepted result if the send succeeded, or a service-unavailable result
    /// if the send threw. Queue Storage has no request/response semantics beyond a send acknowledgement.
    /// </returns>
    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {
            var converter = new QueueStorageContextConverter<TRequest>(new JsonSerializer());
            var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));

            await _middlewarePipeline.HandleAsync(context, _serviceResolver);

            return BenzeneResult.Accepted<TResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message {receiverTopic} to Queue Storage failed", request.Topic);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    /// <summary>
    /// Disposes the client. No-op; the caller owns the <see cref="QueueClient"/>'s lifetime.
    /// </summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
