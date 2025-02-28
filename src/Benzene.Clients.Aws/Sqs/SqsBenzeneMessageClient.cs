﻿using System;
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

public class SqsBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneLogger _logger;
    private readonly string _queueUrl;
    private readonly IMiddlewarePipeline<SqsSendMessageContext> _middlewarePipeline;
    private readonly IServiceResolver _serviceResolver;

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

    public SqsBenzeneMessageClient(string queueUrl, IMiddlewarePipeline<SqsSendMessageContext> middlewarePipeline, IBenzeneLogger logger, IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver;
        _middlewarePipeline = middlewarePipeline;
        _logger = logger;
        _queueUrl = queueUrl;
    }

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

    public void Dispose()
    {
        // Method intentionally left empty.
    }
}