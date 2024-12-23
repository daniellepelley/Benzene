using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.Response;

public interface ISyncResponseHandler<TContext> : IResponseHandler<TContext>
{
    void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}
