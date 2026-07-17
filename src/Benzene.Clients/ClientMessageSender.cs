using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;

namespace Benzene.Clients;

/// <summary>
/// Superseded by <see cref="IBenzeneMessageSender"/>. See
/// <c>work/benzene-clients-redesign-plan.md</c>.
/// </summary>
[Obsolete("Use IBenzeneMessageSender instead - see work/benzene-clients-redesign-plan.md")]
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