using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class MiddlewarePipeline<TContext> : IMiddlewarePipeline<TContext>
{
    private readonly Func<IServiceResolver, IMiddleware<TContext>>[] _items;

    public MiddlewarePipeline(Func<IServiceResolver, IMiddleware<TContext>>[] items)
    {
        _items = items;
    }

    public Task HandleAsync(TContext context, IServiceResolver serviceResolver)
    {
        var chain = CreateChain(context, serviceResolver);
        return chain();
    }

    private Func<Task> CreateChain(TContext context, IServiceResolver serviceResolver)
    {
        var factory = GetMiddlewareFactory(serviceResolver);

        return _items
            .Reverse()
            .Aggregate(() => Task.CompletedTask, (current, middleware) =>
                CreateChainItem(context, factory.Create(serviceResolver, middleware(serviceResolver)).HandleAsync, current));
    }

    private static Func<Task> CreateChainItem(TContext context, Func<TContext, Func<Task>, Task> function, Func<Task> next)
    {
        return () => function(context, next);
    }

    private static IMiddlewareFactory GetMiddlewareFactory(IServiceResolver serviceResolver)
    {
        return serviceResolver.TryGetService<IMiddlewareFactory>() ?? new DefaultMiddlewareFactory(Array.Empty<IMiddlewareWrapper>());
    }
}