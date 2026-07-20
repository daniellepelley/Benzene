using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// A handler middleware pipeline whose <em>structure</em> (the ordered set of
/// <see cref="IHandlerMiddlewareBuilder"/> factories plus the terminal handler invocation) is built
/// once per distinct (builder-set, <typeparamref name="TRequest"/>, <typeparamref name="TResponse"/>)
/// and reused across every message, while the actual middleware and handler <em>instances</em> are
/// resolved fresh per invocation from the per-call <see cref="IServiceResolver"/>.
/// </summary>
/// <typeparam name="TRequest">The strongly-typed request handled by the pipeline.</typeparam>
/// <typeparam name="TResponse">The strongly-typed response produced by the pipeline.</typeparam>
/// <remarks>
/// This mirrors <see cref="MiddlewarePipeline{TContext}"/>'s "structure once, instances per request"
/// split (see that type's remarks): the cached <c>_factories</c> array never closes over a scope or a
/// per-message instance, so a single cached structure is safe to share across concurrently-running
/// records (e.g. a batched fan-out). Only the per-message handler instance is carried on the pipeline
/// itself; everything else is produced inside the returned chain using the resolver passed to
/// <see cref="HandleAsync"/>. Each middleware is created inside its own link (not eagerly), so a
/// short-circuit never touches middleware it doesn't reach, matching <see cref="MiddlewarePipeline{TContext}"/>.
/// </remarks>
internal sealed class HandlerMiddlewarePipeline<TRequest, TResponse>
    : IMiddlewarePipeline<IMessageHandlerContext<TRequest, TResponse>>
    where TRequest : class
{
    private readonly Func<IServiceResolver, IMessageHandler<TRequest, TResponse>, IMiddleware<IMessageHandlerContext<TRequest, TResponse>>?>[] _factories;
    private readonly IMessageHandler<TRequest, TResponse> _messageHandler;

    public HandlerMiddlewarePipeline(
        Func<IServiceResolver, IMessageHandler<TRequest, TResponse>, IMiddleware<IMessageHandlerContext<TRequest, TResponse>>?>[] factories,
        IMessageHandler<TRequest, TResponse> messageHandler)
    {
        _factories = factories;
        _messageHandler = messageHandler;
    }

    /// <inheritdoc />
    public Task HandleAsync(IMessageHandlerContext<TRequest, TResponse> context, IServiceResolver serviceResolver)
    {
        var factory = serviceResolver.TryGetService<IMiddlewareFactory>() ?? new DefaultMiddlewareFactory(Array.Empty<IMiddlewareWrapper>());

        // Fold the cached factories into a chain right-to-left (index loop, no reversed-array alloc).
        // Each middleware is resolved/created only when the chain actually reaches its link, using the
        // per-call resolver - so Scoped/Transient middleware still get a fresh instance per message, a
        // builder that contributes nothing for this type pair (Create returns null) is skipped, and a
        // short-circuit upstream never constructs middleware below it.
        Func<Task> next = static () => Task.CompletedTask;
        for (var i = _factories.Length - 1; i >= 0; i--)
        {
            var createMiddleware = _factories[i];
            var localNext = next;
            next = () =>
            {
                var middleware = createMiddleware(serviceResolver, _messageHandler);
                return middleware == null
                    ? localNext()
                    : factory.Create(serviceResolver, middleware).HandleAsync(context, localNext);
            };
        }

        return next();
    }
}

/// <summary>
/// Per-(<typeparamref name="TRequest"/>, <typeparamref name="TResponse"/>) cache of handler-pipeline
/// <em>structures</em>, keyed by the identity of the builder set they were built from. Using a generic
/// static holder gives each closed type pair its own dictionary, so the remaining key is just the
/// builder set - never (TRequest, TResponse) alone, which would let two pipelines that share a handler
/// but register different handler middleware cross-contaminate each other's chain.
/// </summary>
internal static class HandlerPipelineStructureCache<TRequest, TResponse>
    where TRequest : class
{
    private static readonly ConcurrentDictionary<IHandlerMiddlewareBuilder[], Func<IServiceResolver, IMessageHandler<TRequest, TResponse>, IMiddleware<IMessageHandlerContext<TRequest, TResponse>>?>[]> Cache
        = new(HandlerMiddlewareBuilderSetComparer.Instance);

    public static Func<IServiceResolver, IMessageHandler<TRequest, TResponse>, IMiddleware<IMessageHandlerContext<TRequest, TResponse>>?>[] GetOrAdd(
        IHandlerMiddlewareBuilder[] builders)
        => Cache.GetOrAdd(builders, Build);

    private static Func<IServiceResolver, IMessageHandler<TRequest, TResponse>, IMiddleware<IMessageHandlerContext<TRequest, TResponse>>?>[] Build(
        IHandlerMiddlewareBuilder[] builders)
    {
        var factories = new List<Func<IServiceResolver, IMessageHandler<TRequest, TResponse>, IMiddleware<IMessageHandlerContext<TRequest, TResponse>>?>>(builders.Length + 1);
        foreach (var builder in builders)
        {
            if (builder == null)
            {
                Debug.WriteLine("Null IHandlerMiddlewareBuilder found");
                continue;
            }

            var local = builder;
            factories.Add((resolver, handler) => local.Create(resolver, handler));
        }

        // Terminal step: invoke the handler itself. Bound to the per-message handler at run time, not
        // captured here, so the structure stays handler-agnostic and reusable across messages.
        factories.Add(static (_, handler) => new MessageHandlerMiddleware<TRequest, TResponse>(handler));

        return factories.ToArray();
    }
}

/// <summary>
/// Compares builder sets by ordered element reference-identity, so a fresh array carrying the same
/// startup-registered <see cref="IHandlerMiddlewareBuilder"/> instances (in the same order) hits the
/// same cache entry, while a different pipeline's set (different instances) does not.
/// </summary>
internal sealed class HandlerMiddlewareBuilderSetComparer : IEqualityComparer<IHandlerMiddlewareBuilder[]>
{
    public static readonly HandlerMiddlewareBuilderSetComparer Instance = new();

    public bool Equals(IHandlerMiddlewareBuilder[]? x, IHandlerMiddlewareBuilder[]? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x == null || y == null || x.Length != y.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Length; i++)
        {
            if (!ReferenceEquals(x[i], y[i]))
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode(IHandlerMiddlewareBuilder[] obj)
    {
        var hash = new HashCode();
        foreach (var builder in obj)
        {
            hash.Add(builder == null ? 0 : RuntimeHelpers.GetHashCode(builder));
        }

        return hash.ToHashCode();
    }
}
