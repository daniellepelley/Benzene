using Benzene.Abstractions.Middleware.BenzeneClient;
using Benzene.Results;

namespace Benzene.Clients;

public interface IBenzeneMessageClient : IDisposable
{
    Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request);
}