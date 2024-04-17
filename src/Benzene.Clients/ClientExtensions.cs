using Benzene.Results;

namespace Benzene.Clients;

public static class ClientExtensions
{
    public static Task<IClientResult<TResponse>> SendMessageAsync<TMessage, TResponse>(
        this IBenzeneMessageClient source, string topic, TMessage message)
    {
        return source.SendMessageAsync<TMessage, TResponse>(topic, message, new Dictionary<string, string>());
    }
}
