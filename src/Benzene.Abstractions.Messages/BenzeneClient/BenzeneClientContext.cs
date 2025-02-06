using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Abstractions.Messages.BenzeneClient;

public class BenzeneClientContext<TRequest, TResponse> : IBenzeneClientContext<TRequest, TResponse>
{
    public BenzeneClientContext(IBenzeneClientRequest<TRequest> request)
    {
        Request = request;
    }

    public IBenzeneClientRequest<TRequest> Request { get; }
    public IBenzeneResult<TResponse> Response { get; set; }
}
