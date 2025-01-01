using Benzene.Abstractions.Middleware;

namespace Benzene.Clients;

public class BenzeneClientRequest<TMessage> : IBenzeneClientRequest<TMessage>
{
    public string Topic { get; }
    public TMessage Message { get; }
    public IDictionary<string, string> Headers { get; }

    public BenzeneClientRequest(string topic, TMessage message, IDictionary<string, string> headers)
    {
        Topic = topic;
        Message = message;
        Headers = headers;
    }
}
