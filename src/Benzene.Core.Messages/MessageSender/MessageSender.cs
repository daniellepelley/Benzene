using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Core.Messages.MessageSender;

public class MessageSender<TMessage> :  IMessageSender<TMessage>
{
    private readonly MessageSender<TMessage, Void> _inner;

    public MessageSender(
        IMiddlewarePipeline<IBenzeneClientContext<TMessage, Void>> middlewarePipeline,
        IGetTopic getTopic,
        IServiceResolver serviceResolver)
    {
        _inner = new MessageSender<TMessage, Void>(middlewarePipeline, getTopic, serviceResolver);
    }

    public async Task<IBenzeneResult> SendMessageAsync(TMessage message)
    {
        var result = await _inner.SendMessageAsync(message);
        return result;
    }
}

public class MessageSender<TRequest, TResponse> : IMessageSender<TRequest, TResponse>
{
    private readonly IServiceResolver _serviceResolver;
    private readonly IMiddlewarePipeline<IBenzeneClientContext<TRequest, TResponse>> _middlewarePipeline;
    private readonly IGetTopic _getTopic;

    public MessageSender(
        IMiddlewarePipeline<IBenzeneClientContext<TRequest, TResponse>> middlewarePipeline,
        IGetTopic getTopic,
        IServiceResolver serviceResolver)
    {
        _getTopic = getTopic;
        _middlewarePipeline = middlewarePipeline;
        _serviceResolver = serviceResolver;
    }

    public async Task<IBenzeneResult<TResponse>> SendMessageAsync(TRequest request)
    {
        var topic = _getTopic.GetTopic(typeof(TRequest));
        var context = new BenzeneClientContext<TRequest, TResponse>(new BenzeneClientRequest<TRequest>(topic, request, new Dictionary<string, string>()));
        await _middlewarePipeline.HandleAsync(context, _serviceResolver);
        return context.Response;
    }
}