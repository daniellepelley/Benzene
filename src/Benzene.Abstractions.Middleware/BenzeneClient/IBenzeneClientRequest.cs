namespace Benzene.Abstractions.Middleware.BenzeneClient;

public interface IBenzeneClientRequest<TMessage>
{
    public string Topic { get; }
    public TMessage Message { get; }
    public IDictionary<string, string> Headers { get; }
}