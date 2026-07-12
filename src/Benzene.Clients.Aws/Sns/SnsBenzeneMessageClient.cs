using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Clients.Common;
using Benzene.Core.Middleware;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// A Benzene message client that publishes outgoing messages to an SNS topic.
/// </summary>
public class SnsBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly ILogger<SnsBenzeneMessageClient> _logger;
    private readonly string _topicArn;
    private readonly IServiceResolver _serviceResolver;
    private readonly IMiddlewarePipeline<SnsSendMessageContext> _middlewarePipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnsBenzeneMessageClient"/> class, building a
    /// default single-middleware pipeline around the given SNS client.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to publish to.</param>
    /// <param name="amazonSnsClient">The SNS client to publish with.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public SnsBenzeneMessageClient(string topicArn, IAmazonSimpleNotificationService amazonSnsClient, ILogger<SnsBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _topicArn = topicArn;
        _serviceResolver = serviceResolver;
        _logger = logger;

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<SnsSendMessageContext>(benzeneServiceContainer);
        _middlewarePipeline = middlewarePipelineBuilder
            .UseSnsClient(amazonSnsClient)
            .Build();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SnsBenzeneMessageClient"/> class with a pre-built
    /// middleware pipeline.
    /// </summary>
    /// <param name="topicArn">The ARN of the SNS topic to publish to.</param>
    /// <param name="middlewarePipeline">The built middleware pipeline to publish through.</param>
    /// <param name="logger">The logger used to record send failures.</param>
    /// <param name="serviceResolver">The service resolver used to run the pipeline.</param>
    public SnsBenzeneMessageClient(string topicArn, IMiddlewarePipeline<SnsSendMessageContext> middlewarePipeline, ILogger<SnsBenzeneMessageClient> logger, IServiceResolver serviceResolver)
    {
        _logger = logger;
        _topicArn = topicArn;
        _middlewarePipeline = middlewarePipeline;
        _serviceResolver = serviceResolver;
    }

    /// <summary>
    /// Publishes the request as a message to the configured SNS topic.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The expected response payload type.</typeparam>
    /// <param name="request">The client request to send.</param>
    /// <returns>
    /// A task that resolves to an accepted result if the publish succeeded, a mapped error result based
    /// on the HTTP status code otherwise, or a service-unavailable result if the publish threw.
    /// </returns>
    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {   var converter = new SnsContextConverter<TRequest>(_topicArn, new JsonSerializer());
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
            _logger.LogError(ex, "Sending message {receiverTopic} to {receiver} failed", request.Topic, _topicArn);
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
