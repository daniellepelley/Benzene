using Benzene.Results;

namespace Benzene.Abstractions.Middleware;

public interface IMessageSender<in TRequest>
{
    Task SendMessageAsync(TRequest request);
}

public interface IMessageSender<in TRequest, TResponse> 
{
    Task<IBenzeneResult<TResponse>> SendMessageAsync(TRequest request);
}
