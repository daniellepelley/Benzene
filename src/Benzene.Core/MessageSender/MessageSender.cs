using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.Core.MessageSender;

public class MessageSender<T> : IMessageSender<T>
{
    private readonly IServiceResolver _serviceResolver;
    private readonly IMiddlewarePipeline<IBenzeneClientContext<T, Void>> _middlewarePipeline;
    private readonly IGetTopic _getTopic;

    public MessageSender(
        IMiddlewarePipeline<IBenzeneClientContext<T, Void>> middlewarePipeline,
        IGetTopic getTopic,
        IServiceResolver serviceResolver)
    {
        _getTopic = getTopic;
        _middlewarePipeline = middlewarePipeline;
        _serviceResolver = serviceResolver;
    }

    public async Task<IBenzeneResult> SendMessageAsync(T request)
    {
        var topic = _getTopic.GetTopic(typeof(T));
        var context = new BenzeneClientContext<T, Void>(new BenzeneClientRequest<T>(topic, request, new Dictionary<string, string>()));
        await _middlewarePipeline.HandleAsync(context, _serviceResolver);
        return context.Response;
    }
}