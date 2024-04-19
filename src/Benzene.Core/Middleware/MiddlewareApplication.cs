using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class MiddlewareApplication<TEvent, TContext, TResult> : IMiddlewareApplication<TEvent, TResult> //where TContext : IHasMessageResult
{
    private readonly Func<TEvent, TContext> _mapper;
    private readonly IMiddlewarePipeline<TContext> _pipeline;
    private readonly Func<TContext, TResult> _resultMapper;

    public MiddlewareApplication(IMiddlewarePipeline<TContext> pipeline, Func<TEvent, TContext> mapper, Func<TContext, TResult> resultMapper)
    {
        _resultMapper = resultMapper;
        _mapper = mapper;
        _pipeline = pipeline;
    }

    public async Task<TResult> HandleAsync(TEvent @event, IServiceResolver serviceResolver)
    {
        var context = _mapper(@event);
        await _pipeline.HandleAsync(context, serviceResolver);
        return _resultMapper(context);
    }
}

public class MiddlewareApplication<TEvent, TContext> : IMiddlewareApplication<TEvent> //where TContext : IHasMessageResult
{
    private readonly Func<TEvent, TContext> _mapper;
    private readonly IMiddlewarePipeline<TContext> _pipelineBuilder;

    public MiddlewareApplication(IMiddlewarePipeline<TContext> pipelineBuilder, Func<TEvent, TContext> mapper)
    {
        _mapper = mapper;
        _pipelineBuilder = pipelineBuilder;
    }

    public async Task HandleAsync(TEvent @event, IServiceResolver serviceResolver)
    {
        var context = _mapper(@event);
        await _pipelineBuilder.HandleAsync(context, serviceResolver);
    }
}
