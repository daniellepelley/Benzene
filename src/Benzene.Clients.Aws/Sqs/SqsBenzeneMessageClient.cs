using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.Middleware;
using Benzene.Clients.Aws.Common;
using Benzene.Core.Middleware;
using Benzene.Results;

namespace Benzene.Clients.Aws.Sqs;

public class SqsBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneLogger _logger;
    private readonly string _queueUrl;
    private readonly EntryPointMiddlewareApplication<SendMessageRequest, SendMessageResponse> _entryPointMiddlewareApplication;
    private readonly IClientRequestMapper<SendMessageRequest> _requestMapper;

    public SqsBenzeneMessageClient(string queueUrl, IAmazonSQS amazonSqsClient, IBenzeneLogger logger)
    {
        _logger = logger;
        _requestMapper = new SqsClientRequestMapper(queueUrl, new JsonSerializer());

        var benzeneServiceContainer = new NullBenzeneServiceContainer();
        var middlewarePipelineBuilder = new MiddlewarePipelineBuilder<SqsSendMessageContext>(benzeneServiceContainer);
        var middlewarePipeline = middlewarePipelineBuilder
            .UseSqsClient(amazonSqsClient)
            .Build();

        var application = new MiddlewareApplication<SendMessageRequest, SqsSendMessageContext, SendMessageResponse>(
            middlewarePipeline, request => new SqsSendMessageContext(request), context => context.Response);
        _entryPointMiddlewareApplication = new EntryPointMiddlewareApplication<SendMessageRequest, SendMessageResponse>(application, benzeneServiceContainer.CreateServiceResolverFactory());
    }

    public SqsBenzeneMessageClient(string queueUrl, IMiddlewarePipeline<SqsSendMessageContext> middlewarePipeline, IBenzeneLogger logger)
    {
        _logger = logger;
        _queueUrl = queueUrl;
        var benzeneServiceContainer =  new NullBenzeneServiceContainer();

        var application = new MiddlewareApplication<SendMessageRequest, SqsSendMessageContext, SendMessageResponse>(
            middlewarePipeline, request => new SqsSendMessageContext(request), context => context.Response);
        _entryPointMiddlewareApplication = new EntryPointMiddlewareApplication<SendMessageRequest, SendMessageResponse>(application, benzeneServiceContainer.CreateServiceResolverFactory());
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