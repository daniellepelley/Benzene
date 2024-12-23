using Benzene.Results;

namespace Benzene.Clients;

public static class ClientExtensions
{
    public static Task<IBenzeneResult<TResponse>> SendMessageAsync<TMessage, TResponse>(
        this IBenzeneMessageClient source, string topic, TMessage message)
    {
        return source.SendMessageAsync<TMessage, TResponse>(topic, message, new Dictionary<string, string>());
    }

    public static Task<IBenzeneResult<TResponse>> SendMessageAsync<TMessage, TResponse>(
        this IBenzeneMessageClient source, string topic, TMessage message, IDictionary<string, string> headers)
    {
        return source.SendMessageAsync<TMessage, TResponse>(new BenzeneClientRequest<TMessage>(topic, message, headers));
    }

}
