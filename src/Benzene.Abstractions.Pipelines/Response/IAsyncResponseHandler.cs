using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Abstractions.Response;

public interface IAsyncResponseHandler<TContext> : IResponseHandler<TContext>
{
    Task HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}
