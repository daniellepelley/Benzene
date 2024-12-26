namespace Benzene.Abstractions.MessageHandlers.Response;

public interface ISyncResponseHandler<TContext> : IResponseHandler<TContext>
{
    void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}
