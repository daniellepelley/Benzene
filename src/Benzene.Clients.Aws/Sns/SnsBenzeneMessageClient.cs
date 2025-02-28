using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Clients.Common;
using Benzene.Core.Middleware;
using Benzene.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Sns;

public class SnsBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneLogger _logger;
    private readonly string _topicArn;
    private readonly IServiceResolver _serviceResolver;
    private readonly IMiddlewarePipeline<SnsSendMessageContext> _middlewarePipeline;

    public SnsBenzeneMessageClient(string topicArn, IAmazonSimpleNotificationService amazonSnsClient, IBenzeneLogger logger, IServiceResolver serviceResolver)
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

    public SnsBenzeneMessageClient(string topicArn, IMiddlewarePipeline<SnsSendMessageContext> middlewarePipeline, IBenzeneLogger logger)
    {
        _logger = logger;
        _topicArn = topicArn;
        _middlewarePipeline = middlewarePipeline;
    }

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

    public void Dispose()
    {
        // Method intentionally left empty.
    }
}