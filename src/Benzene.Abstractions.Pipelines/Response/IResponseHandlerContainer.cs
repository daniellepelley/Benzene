using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.Response;

public interface IResponseHandlerContainer<TContext>
{
    Task HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}