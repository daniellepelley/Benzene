using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Middleware.BenzeneClient;
using Benzene.Clients.Common;
using Benzene.Core.Middleware;
using Benzene.Results;

namespace Benzene.Clients.Aws.Sns;

public class SnsBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneLogger _logger;
    private readonly string _queueUrl;
    private readonly EntryPointMiddlewareApplication<PublishRequest, PublishResponse> _entryPointMiddlewareApplication;
    private readonly IClientRequestMapper<PublishRequest> _requestMapper;

    public SnsBenzeneMessageClient(string queueUrl, IAmazonSimpleNotificationService amazonSnsClient, IBenzeneLogger logger)
    {
        _logger = logger;
        _requestMapper = new SnsClientRequestMapper(queueUrl, new JsonSerializer());

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<SnsSendMessageContext>(benzeneServiceContainer);
        var middlewarePipeline = middlewarePipelineBuilder
            .UseSnsClient(amazonSnsClient)
            .Build();

        var application = new MiddlewareApplication<PublishRequest, SnsSendMessageContext, PublishResponse>(
            middlewarePipeline, request => new SnsSendMessageContext(request), context => context.Response);
        _entryPointMiddlewareApplication = new EntryPointMiddlewareApplication<PublishRequest, PublishResponse>(application, benzeneServiceContainer.CreateServiceResolverFactory());
    }

    public SnsBenzeneMessageClient(string queueUrl, IMiddlewarePipeline<SnsSendMessageContext> middlewarePipeline, IBenzeneLogger logger)
    {
        _logger = logger;
        _queueUrl = queueUrl;
        var benzeneServiceContainer = new NullBenzeneServiceContainer();

        var application = new MiddlewareApplication<PublishRequest, SnsSendMessageContext, PublishResponse>(
            middlewarePipeline, request => new SnsSendMessageContext(request), context => context.Response);
        _entryPointMiddlewareApplication = new EntryPointMiddlewareApplication<PublishRequest, PublishResponse>(application, benzeneServiceContainer.CreateServiceResolverFactory());
    }

    public async Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        try
        {
            var response = await _entryPointMiddlewareApplication.SendAsync(_requestMapper.CreateRequest(request));

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

    public void Dispose()
    {
        // Method intentionally left empty.
    }
}