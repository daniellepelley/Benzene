﻿using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.MessageHandlers;

public class PipelineMessageHandlerWrapper : IMessageHandlerWrapper
{
    private readonly IHandlerPipelineBuilder _handlerPipelineBuilder;
    private readonly IServiceResolver _serviceResolver;

    public PipelineMessageHandlerWrapper(IHandlerPipelineBuilder handlerPipelineBuilder, IServiceResolver serviceResolver)
    {
        _handlerPipelineBuilder = handlerPipelineBuilder;
        _serviceResolver = serviceResolver;
    }

    public IMessageHandler<TRequest, TResponse> Wrap<TRequest, TResponse>(ITopic topic, IMessageHandler<TRequest> messageHandler)
        where TRequest : class
    {
        var pipeline = _handlerPipelineBuilder.Create(new MessageHandlerNoResultWrapper<TRequest, TResponse>(messageHandler), _serviceResolver);
        return new PipelineMessageHandler<TRequest, TResponse>(topic, pipeline, _serviceResolver);
    }

    public IMessageHandler<TRequest, TResponse> Wrap<TRequest, TResponse>(ITopic topic, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class
    {
        var pipeline = _handlerPipelineBuilder.Create(messageHandler, _serviceResolver);
        return new PipelineMessageHandler<TRequest, TResponse>(topic, pipeline, _serviceResolver);
    }
}
