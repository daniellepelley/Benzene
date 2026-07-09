using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class MiddlewareApplication<TEvent, TContext, TResult>(
    IMiddlewarePipeline<TContext> pipeline,
    Func<TEvent, TContext> mapper,
    Func<TContext, TResult> resultMapper)
    : IMiddlewareApplication<TEvent, TResult>
{
    public async Task<TResult> HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var context = mapper(@event);
        await pipeline.HandleAsync(context, serviceResolverFactory.CreateScope());
        return resultMapper(context);
    }
}

public class MiddlewareApplication<TEvent, TContext>(
    IMiddlewarePipeline<TContext> pipeline,
    Func<TEvent, TContext> mapper)
    : IMiddlewareApplication<TEvent>
{
    public async Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var context = mapper(@event);
        await pipeline.HandleAsync(context, serviceResolverFactory.CreateScope());
    }
}
