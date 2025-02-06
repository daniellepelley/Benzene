using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Clients;

public interface IBenzeneMessageClient : IDisposable
{
    Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request);
}