using Benzene.Abstractions.DI;
using Benzene.Results;
using Void = Benzene.Results.Void;

namespace Benzene.Abstractions.Middleware;

public interface IMessageSenderBuilder
{
    void CreateSender<T>(Action<IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>>> action);
}

public interface IGetTopic
{
    string GetTopic(Type type);
}

public interface IBenzeneClientRequest<TMessage>
{
    public string Topic { get; }
    public TMessage Message { get; }
    public IDictionary<string, string> Headers { get; }
}

public interface IBenzeneClientContext<TRequest, TResponse>
{
    IBenzeneClientRequest<TRequest> Request { get; }
    IBenzeneResult<TResponse> Response { get; set; }
}

public class BenzeneClientContext<TRequest, TResponse> : IBenzeneClientContext<TRequest, TResponse>
{
    public BenzeneClientContext(IBenzeneClientRequest<TRequest> request)
    {
        Request = request;
    }

    public IBenzeneClientRequest<TRequest> Request { get; }
    public IBenzeneResult<TResponse> Response { get; set; }
}
