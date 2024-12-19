namespace Benzene.Abstractions.MessageHandlers;

public interface IMessageHandlerWrapper
{
    IMessageHandler<TRequest, TResponse> Wrap<TRequest, TResponse>(ITopic topic, IMessageHandler<TRequest> messageHandler)
        where TRequest : class;
    IMessageHandler<TRequest, TResponse> Wrap<TRequest, TResponse>(ITopic topic, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class;
}
