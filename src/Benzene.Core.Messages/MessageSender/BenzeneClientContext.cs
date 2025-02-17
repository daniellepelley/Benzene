using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;

namespace Benzene.Core.Messages.MessageSender;

public class BenzeneClientContext<TRequest, TResponse> : IBenzeneClientContext<TRequest, TResponse>
{
    public BenzeneClientContext(IBenzeneClientRequest<TRequest> request)
    {
        Request = request;
    }

    public IBenzeneClientRequest<TRequest> Request { get; }
    public IBenzeneResult<TResponse> Response { get; set; }
}