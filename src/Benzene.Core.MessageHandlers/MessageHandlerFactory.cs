using Benzene.Abstractions.DI;
using Benzene.Abstractions.Logging;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.Exceptions;

namespace Benzene.Core.MessageHandlers;

public class MessageHandlerFactory : IMessageHandlerFactory
{
    private readonly IMessageHandlerWrapper _messageHandlerWrapper;
    private readonly IServiceResolver _serviceResolver;
    private readonly IBenzeneLogger _logger;

    public MessageHandlerFactory(IServiceResolver serviceResolver, IMessageHandlerWrapper messageHandlerWrapper, IBenzeneLogger logger)
    {
        _logger = logger;
        _serviceResolver = serviceResolver;
        _messageHandlerWrapper = messageHandlerWrapper;
    }

    public IMessageHandler Create(IMessageHandlerDefinition messageHandlerDefinition)
    {
        return CreateMessageHandler(new Topic(messageHandlerDefinition.Topic.Id, messageHandlerDefinition.Topic.Version), messageHandlerDefinition.HandlerType, messageHandlerDefinition.RequestType,
            messageHandlerDefinition.ResponseType);
    }

    private IMessageHandler CreateMessageHandler(ITopic topic, Type messageHandlerType, Type requestType, Type responseType)
    {
        var method = GetType().GetMethod("CreateMessageHandlerByType");
        if (method == null)
        {
            throw new BenzeneException("Method CreateMessageHandlerByType is missing");
        }

        var genericMethod = method.MakeGenericMethod(messageHandlerType, requestType, responseType);
        return genericMethod.Invoke(this, new object[]{ topic }) as IMessageHandler;
    }

    public IMessageHandler CreateMessageHandlerByType<TMessageHandler, TRequest, TResponse>(ITopic topic)
        where TMessageHandler : class
        where TRequest : class
    {
        var messageHandler = _serviceResolver.GetService<TMessageHandler>();

        switch (messageHandler)
        {
            case IMessageHandler<TRequest, TResponse> handlerWithResponse:
            {
                var wrapped = _messageHandlerWrapper.Wrap(topic, handlerWithResponse);
                return new MessageHandler<TRequest, TResponse>(wrapped, _logger);
            }
            case IMessageHandler<TRequest> handlerNoResponse:
            {
                var wrapped = _messageHandlerWrapper.Wrap<TRequest, TResponse>(topic, handlerNoResponse);
                return new MessageHandler<TRequest, TResponse>(wrapped, _logger);
            }
            default:
                return null;
        }
    }
}
