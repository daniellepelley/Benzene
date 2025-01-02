using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class MiddlewareMultiApplication<TEvent, TContext, TResult> : IMiddlewareApplication<TEvent, TResult[]>
{
    private readonly Func<TEvent, TContext[]> _mapper;
    private readonly IMiddlewarePipeline<TContext> _pipelineBuilder;
    private readonly Func<TContext, TResult> _resultMapper;

    public MiddlewareMultiApplication(IMiddlewarePipeline<TContext> pipelineBuilder, Func<TEvent, TContext[]> mapper,
        Func<TContext, TResult> resultMapper)
    {
        _resultMapper = resultMapper;
        _mapper = mapper;
        _pipelineBuilder = pipelineBuilder;
    }

    public Task<TResult[]> HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var tasks = _mapper(@event).Select(async context =>
            {
                using var scope = serviceResolverFactory.CreateScope();
                await _pipelineBuilder.HandleAsync(context, scope);
                return _resultMapper(context);
            })
            .ToArray();

        return Task.WhenAll(tasks);
    }
}

public class MiddlewareMultiApplication<TEvent, TContext> : IMiddlewareApplication<TEvent>
{
    private readonly Func<TEvent, TContext[]> _mapper;
    private readonly IMiddlewarePipeline<TContext> _pipeline;

    public MiddlewareMultiApplication(IMiddlewarePipeline<TContext> pipeline, Func<TEvent, TContext[]> mapper)
    {
        _mapper = mapper;
        _pipeline = pipeline;
    }

    public Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var tasks = _mapper(@event).Select(async context =>
            {
                using var scope = serviceResolverFactory.CreateScope();
                await _pipeline.HandleAsync(context, scope);
            })
            .ToArray();

        return Task.WhenAll(tasks);
    }
}


