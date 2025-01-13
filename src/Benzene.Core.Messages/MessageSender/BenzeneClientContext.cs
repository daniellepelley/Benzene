using Benzene.Abstractions.Middleware.BenzeneClient;
using Benzene.Results;

namespace Benzene.Core.MessageSender;

public class BenzeneClientContext<TRequest, TResponse> : IBenzeneClientContext<TRequest, TResponse>
{
    public BenzeneClientContext(IBenzeneClientRequest<TRequest> request)
    {
        Request = request;
    }

    public IBenzeneClientRequest<TRequest> Request { get; }
    public IBenzeneResult<TResponse> Response { get; set; }
}