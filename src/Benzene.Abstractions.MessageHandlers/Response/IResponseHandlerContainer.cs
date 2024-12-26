namespace Benzene.Abstractions.MessageHandlers.Response;

public interface IResponseHandlerContainer<TContext>
{
    Task HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}