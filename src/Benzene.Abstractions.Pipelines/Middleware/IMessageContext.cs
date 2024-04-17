using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Abstractions.Middleware;

public interface IMessageContext<TRequest, TResponse>
{
    ITopic Topic { get; }
    TRequest Request { get; }
    IServiceResult<TResponse> Response { get; set; }
}