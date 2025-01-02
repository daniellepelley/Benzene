using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class MiddlewareApplication<TEvent, TContext, TResult> : IMiddlewareApplication<TEvent, TResult> 
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

    public async Task<TResult> HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var context = _mapper(@event);
        await _pipeline.HandleAsync(context, serviceResolverFactory.CreateScope());
        return _resultMapper(context);
    }
}

public class MiddlewareApplication<TEvent, TContext> : IMiddlewareApplication<TEvent>
{
    private readonly Func<TEvent, TContext> _mapper;
    private readonly IMiddlewarePipeline<TContext> _pipeline;

    public MiddlewareApplication(IMiddlewarePipeline<TContext> pipeline, Func<TEvent, TContext> mapper)
    {
        _mapper = mapper;
        _pipeline = pipeline;
    }

    public async Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var context = _mapper(@event);
        await _pipeline.HandleAsync(context, serviceResolverFactory.CreateScope());
    }
}
