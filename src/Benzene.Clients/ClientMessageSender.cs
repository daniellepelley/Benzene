using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Results;

namespace Benzene.Clients;

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