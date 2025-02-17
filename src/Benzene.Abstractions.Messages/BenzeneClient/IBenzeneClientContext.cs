using Benzene.Abstractions.Results;

namespace Benzene.Abstractions.Messages.BenzeneClient;

public interface IBenzeneClientContext<TRequest, TResponse>
{
    IBenzeneClientRequest<TRequest> Request { get; }
    IBenzeneResult<TResponse> Response { get; set; }
}