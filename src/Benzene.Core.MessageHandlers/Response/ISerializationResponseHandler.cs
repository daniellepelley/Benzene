using Benzene.Abstractions.MessageHandlers;

namespace Benzene.Core.MessageHandlers.Response;

public interface ISerializationResponseHandler<TContext> where TContext : class
{
    void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult, IBodySerializer bodySerializer);
}