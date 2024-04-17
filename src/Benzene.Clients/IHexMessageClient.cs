using Benzene.Results;

namespace Benzene.Clients
{
    public interface IBenzeneMessageClient : IDisposable
    {
        Task<IClientResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message, IDictionary<string, string> headers);
    }
}
