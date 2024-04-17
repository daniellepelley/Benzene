using Benzene.Results;

namespace Benzene.Abstractions.MessageHandling;

public interface IMessageSender<TRequest>
{
    Task SendMessageAsync(TRequest request);
}

public interface IMessageSender<TRequest, TResponse> 
{
    Task<IResult<TResponse>> SendMessageAsync(TRequest request);
}
