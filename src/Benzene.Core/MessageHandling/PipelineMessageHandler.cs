using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandling;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Core.Message;
using Benzene.Results;

namespace Benzene.Core.MessageHandling;

public class PipelineMessageHandler<TRequest, TResponse> : IMessageHandler<TRequest, TResponse>
{
    private readonly IMiddlewarePipeline<IMessageContext<TRequest, TResponse>> _pipelineBuilder;
    private readonly IServiceResolver _serviceResolver;
    private readonly ITopic _topic;

    public PipelineMessageHandler(ITopic topic, IMiddlewarePipeline<IMessageContext<TRequest, TResponse>> pipelineBuilder, IServiceResolver serviceResolver)
    {
        _topic = topic;
        _serviceResolver = serviceResolver;
        _pipelineBuilder = pipelineBuilder;
    }

    public async Task<IServiceResult<TResponse>> HandleAsync(TRequest request)
    {
        var context = new MessageContext<TRequest, TResponse>(_topic, request);
        await _pipelineBuilder.HandleAsync(context, _serviceResolver);
        return context.Response;
    }
}