using Benzene.Results;

namespace Benzene.Abstractions.Messages;

public interface IMessageSender<in TRequest>
{
    Task<IBenzeneResult> SendMessageAsync(TRequest message);
}

public interface IMessageSender<in TRequest, TResponse> 
{
    Task<IBenzeneResult<TResponse>> SendMessageAsync(TRequest request);
}
