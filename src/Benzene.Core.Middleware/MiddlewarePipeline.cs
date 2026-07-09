using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

public class MiddlewarePipeline<TContext>(Func<IServiceResolver, IMiddleware<TContext>>[] items)
    : IMiddlewarePipeline<TContext>
{
    private Func<TContext, IServiceResolver, Task>? _cachedChain;

    public Task HandleAsync(TContext context, IServiceResolver serviceResolver)
    {
        if (_cachedChain != null)
        {
            return _cachedChain(context, serviceResolver);
        }

        var chain = CreateChain(context, serviceResolver);
        return chain();
    }

    private Func<Task> CreateChain(TContext context, IServiceResolver serviceResolver)
    {
        var factory = GetMiddlewareFactory(serviceResolver);

        return items
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