using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Core.Middleware;

/// <summary>
/// Provides the core implementation of a middleware pipeline that executes middleware components in sequence.
/// </summary>
/// <typeparam name="TContext">The context type that flows through the pipeline.</typeparam>
/// <remarks>
/// This class builds a chain of middleware components and executes them in the order they were registered.
/// Middleware is applied via the configured middleware factory, which can add cross-cutting concerns like
/// logging or timing. The pipeline uses functional composition to link middleware together efficiently.
/// </remarks>
public class MiddlewarePipeline<TContext>(Func<IServiceResolver, IMiddleware<TContext>>[] items)
    : IMiddlewarePipeline<TContext>
{
    private Func<TContext, IServiceResolver, Task>? _cachedChain;

    /// <summary>
    /// Executes the middleware pipeline with the given context.
    /// </summary>
    /// <param name="context">The context to process through the pipeline.</param>
    /// <param name="serviceResolver">The service resolver for dependency resolution.</param>
    /// <returns>A task representing the asynchronous pipeline execution.</returns>
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