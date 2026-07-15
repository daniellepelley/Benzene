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
/// The composed chain itself can't be cached across invocations - middleware instances are resolved fresh
/// from <c>serviceResolver</c> on every call (so a middleware registered as Scoped/Transient in DI gets a
/// new instance per request, as intended), and the chain closes over the current <c>context</c>.
/// What's registration-time-fixed is the middleware order, so that part is precomputed once below.
/// </remarks>
public class MiddlewarePipeline<TContext>(Func<IServiceResolver, IMiddleware<TContext>>[] items)
    : IMiddlewarePipeline<TContext>
{
    // Reversed once at construction (order never changes after that) instead of re-reversing `items`
    // on every HandleAsync call - Enumerable.Reverse() has to buffer and copy the whole sequence, so
    // doing that per-request was pure waste for a pipeline that's typically invoked many times.
    private readonly Func<IServiceResolver, IMiddleware<TContext>>[] _reversedItems = items.Reverse().ToArray();

    /// <summary>
    /// Executes the middleware pipeline with the given context.
    /// </summary>
    /// <param name="context">The context to process through the pipeline.</param>
    /// <param name="serviceResolver">The service resolver for dependency resolution.</param>
    /// <returns>A task representing the asynchronous pipeline execution.</returns>
    public Task HandleAsync(TContext context, IServiceResolver serviceResolver)
    {
        var chain = CreateChain(context, serviceResolver);
        return chain();
    }

    private Func<Task> CreateChain(TContext context, IServiceResolver serviceResolver)
    {
        var factory = GetMiddlewareFactory(serviceResolver);

        return _reversedItems
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