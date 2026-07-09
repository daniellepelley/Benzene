using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class MiddlewareMultiApplication<TEvent, TContext, TResult>(
    IMiddlewarePipeline<TContext> pipelineBuilder,
    Func<TEvent, TContext[]> mapper,
    Func<TContext, TResult> resultMapper)
    : IMiddlewareApplication<TEvent, TResult[]>
{
    public Task<TResult[]> HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var tasks = mapper(@event).Select(async context =>
            {
                using var scope = serviceResolverFactory.CreateScope();
                await pipelineBuilder.HandleAsync(context, scope);
                return resultMapper(context);
            })
            .ToArray();

        return Task.WhenAll(tasks);
    }
}

public class MiddlewareMultiApplication<TEvent, TContext>(
    IMiddlewarePipeline<TContext> pipeline,
    Func<TEvent, TContext[]> mapper)
    : IMiddlewareApplication<TEvent>
{
    public Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
    {
        var tasks = mapper(@event).Select(async context =>
            {
                using var scope = serviceResolverFactory.CreateScope();
                await pipeline.HandleAsync(context, scope);
            })
            .ToArray();

        return Task.WhenAll(tasks);
    }
}


