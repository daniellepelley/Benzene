using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients;

public interface IBenzeneMessageClient : IDisposable
{
    Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request);
}

public static class BenzeneMessageClientExtensions
{
}