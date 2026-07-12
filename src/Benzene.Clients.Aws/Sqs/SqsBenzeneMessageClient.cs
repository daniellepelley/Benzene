using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.SQS;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Clients.Common;
using Benzene.Core.Middleware;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// A Benzene message client that sends outgoing messages to an SQS queue.
/// </summary>
public class SqsBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneLogger _logger;
    private readonly string _queueUrl;
    private readonly IMiddlewarePipeline<SqsSendMessageContext> _middlewarePipeline;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsBenzeneMessageClient"/> class, building a
    /// default single-middleware pipeline around the given SQS client.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="amazonSqsClient">The SQS client to send with.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public SqsBenzeneMessageClient(string queueUrl, IAmazonSQS amazonSqsClient, IBenzeneLogger logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _queueUrl = queueUrl;
        _logger = logger;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<SqsSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseSqsClient(amazonSqsClient)
            .Build();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsBenzeneMessageClient"/> class with a pre-built
    /// middleware pipeline.
    /// </summary>
    /// <param name="queueUrl">The URL of the queue to send to.</param>
    /// <param name="middlewarePipeline">The built middleware pipeline to send through.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public SqsBenzeneMessageClient(string queueUrl, IMiddlewarePipeline<SqsSendMessageContext> middlewarePipeline, IBenzeneLogger logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _middlewarePipeline = middlewarePipeline;
        _logger = logger;
        _queueUrl = queueUrl;
    }

    /// <summary>
    /// Sends the request as a message to the configured SQS queue.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="request">The client request to send.</param>
    /// <returns>
    /// A task that resolves to an accepted result if the send succeeded, a mapped error result based on
    /// the HTTP status code otherwise, or a service-unavailable result if the send threw.
    /// </returns>
    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {
            var converter = new SqsContextConverter<TRequest>(_queueUrl, new JsonSerializer());
            var context = await converter.CreateRequestAsync(new BenzeneClientContext<TRequest, Void>(request));

            await _middlewarePipeline.HandleAsync(context, _serviceResolver);

            var response = context.Response;

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return BenzeneResult.Accepted<TResponse>();
            }

            return BenzeneResultHttpMapper.Map<TResponse>(response.HttpStatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sending message {receiverTopic} to {receiver} failed", request.Topic, _queueUrl);
            return BenzeneResult.ServiceUnavailable<TResponse>(ex.Message);
        }
    }

    /// <summary>
    /// Disposes the client. No-op; the client holds no disposable resources of its own.
    /// </summary>
    public void Dispose()
    {
        // Method intentionally left empty.
    }
}
