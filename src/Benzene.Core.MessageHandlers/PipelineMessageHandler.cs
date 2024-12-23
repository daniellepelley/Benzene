using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Results;

namespace Benzene.Core.MessageHandlers;

public class PipelineMessageHandler<TRequest, TResponse> : IMessageHandler<TRequest, TResponse>
{
    private readonly IMiddlewarePipeline<IMessageContext<TRequest, TResponse>> _pipeline;
    private readonly IServiceResolver _serviceResolver;
    private readonly ITopic _topic;

    public PipelineMessageHandler(ITopic topic, IMiddlewarePipeline<IMessageContext<TRequest, TResponse>> pipeline, IServiceResolver serviceResolver)
    {
        _topic = topic;
        _serviceResolver = serviceResolver;
        _pipeline = pipeline;
    }

    public async Task<IBenzeneResult<TResponse>> HandleAsync(TRequest request)
    {
        var context = new MessageContext<TRequest, TResponse>(_topic, request);
        await _pipeline.HandleAsync(context, _serviceResolver);
        return context.Response;
    }
}