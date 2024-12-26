namespace Benzene.Abstractions.MessageHandlers.Response;

public interface IAsyncResponseHandler<TContext> : IResponseHandler<TContext>
{
    Task HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}
