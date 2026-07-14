using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IMessageHandlerWrapper"/> implementation: builds the handler's middleware
/// pipeline via <see cref="IHandlerPipelineBuilder"/> and returns it as a <see cref="PipelineMessageHandler{TRequest,TResponse}"/>,
/// wrapping no-response handlers with <see cref="MessageHandlerNoResultWrapper{TRequest,TResponse}"/>
/// first so both handler shapes end up going through the same pipeline machinery.
/// </summary>
public class PipelineMessageHandlerWrapper : IMessageHandlerWrapper
{
    private readonly IHandlerPipelineBuilder _handlerPipelineBuilder;
    private readonly IServiceResolver _serviceResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineMessageHandlerWrapper"/> class.
    /// </summary>
    /// <param name="handlerPipelineBuilder">Builds the middleware pipeline wrapping each handler.</param>
    /// <param name="serviceResolver">Resolver passed to the pipeline builder and each resulting pipeline.</param>
    public PipelineMessageHandlerWrapper(IHandlerPipelineBuilder handlerPipelineBuilder, IServiceResolver serviceResolver)
    {
        _handlerPipelineBuilder = handlerPipelineBuilder;
        _serviceResolver = serviceResolver;
    }

    /// <inheritdoc />
    public IMessageHandler<TRequest, TResponse> Wrap<TRequest, TResponse>(ITopic topic, IMessageHandler<TRequest> messageHandler)
        where TRequest : class
    {
        var pipeline = _handlerPipelineBuilder.Create(new MessageHandlerNoResultWrapper<TRequest, TResponse>(messageHandler), _serviceResolver);
        return new PipelineMessageHandler<TRequest, TResponse>(topic, pipeline, _serviceResolver, messageHandler.GetType());
    }

    /// <inheritdoc />
    public IMessageHandler<TRequest, TResponse> Wrap<TRequest, TResponse>(ITopic topic, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class
    {
        var pipeline = _handlerPipelineBuilder.Create(messageHandler, _serviceResolver);
        return new PipelineMessageHandler<TRequest, TResponse>(topic, pipeline, _serviceResolver, messageHandler.GetType());
    }
}
