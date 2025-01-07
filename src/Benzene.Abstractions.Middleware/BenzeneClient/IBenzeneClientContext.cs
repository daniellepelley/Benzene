using Benzene.Results;

namespace Benzene.Abstractions.Middleware.BenzeneClient;

public interface IBenzeneClientContext<TRequest, TResponse>
{
    IBenzeneClientRequest<TRequest> Request { get; }
    IBenzeneResult<TResponse> Response { get; set; }
}