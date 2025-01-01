using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.Clients;

public interface IBenzeneMessageClient : IDisposable
{
    Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request);
}

public class ClientMessageSender<TRequest, TResponse> : IMessageSender<TRequest, TResponse>
{
    private readonly IClientMessageRouter _clientMessageRouter;
    private readonly IGetTopic _getTopic;

    public ClientMessageSender(IClientMessageRouter clientMessageRouter, IGetTopic getTopic)
    {
        _getTopic = getTopic;
        _clientMessageRouter = clientMessageRouter;
    }
    
    public Task<IBenzeneResult<TResponse>> SendMessageAsync(TRequest request)
    {
        var client = _clientMessageRouter.GetClient<TRequest>();
        var topic = _getTopic.GetTopic(typeof(TRequest));
        return client.SendMessageAsync<TRequest, TResponse>(
            new BenzeneClientRequest<TRequest>(topic, request, new Dictionary<string, string>()));
    }
}

public interface IClientMessageRouter
{
    IBenzeneMessageClient GetClient<TRequest>();
}

public interface IGetTopic
{
    string GetTopic(Type type);
}