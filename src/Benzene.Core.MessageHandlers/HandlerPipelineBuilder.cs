using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Default <see cref="IHandlerPipelineBuilder"/> implementation: assembles a handler's middleware
/// pipeline from every registered <see cref="IHandlerMiddlewareBuilder"/> (e.g. filters), followed by
/// a terminal <see cref="MessageHandlerMiddleware{TRequest,TResponse}"/> that invokes the handler itself.
/// </summary>
/// <remarks>
/// The pipeline <em>structure</em> is fixed once the builder set is known (after startup); only the
/// middleware and handler <em>instances</em> are genuinely per-scope. So rather than reassembling the
/// whole chain on every dispatched message, <see cref="Create{TRequest, TResponse}"/> resolves a
/// cached, handler-agnostic structure from <see cref="HandlerPipelineStructureCache{TRequest, TResponse}"/>
/// (keyed by the builder set, per type pair) and wraps it with the current message's handler in a
/// <see cref="HandlerMiddlewarePipeline{TRequest, TResponse}"/> - mirroring how
/// <see cref="MiddlewarePipeline{TContext}"/> precomputes order once and resolves instances per request.
/// </remarks>
public class HandlerPipelineBuilder : IHandlerPipelineBuilder
{
    // Kept as an immutable array (replaced wholesale on Add, never mutated in place) so it doubles as a
    // stable cache key: two messages on the same pipeline present the same builder-set instances, so the
    // cached structure is reused rather than rebuilt.
    private IHandlerMiddlewareBuilder[] _routerMiddlewareBuilders;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerPipelineBuilder"/> class.
    /// </summary>
    /// <param name="routerMiddlewareBuilders">The handler middleware builders to include in every pipeline built from now on.</param>
    public HandlerPipelineBuilder(IEnumerable<IHandlerMiddlewareBuilder> routerMiddlewareBuilders)
    {
        _routerMiddlewareBuilders = routerMiddlewareBuilders as IHandlerMiddlewareBuilder[]
            ?? routerMiddlewareBuilders.ToArray();
    }

    /// <inheritdoc />
    public void Add(params IHandlerMiddlewareBuilder[] routerMiddlewareBuilders)
    {
        _routerMiddlewareBuilders = _routerMiddlewareBuilders.Length == 0
            ? routerMiddlewareBuilders
            : _routerMiddlewareBuilders.Concat(routerMiddlewareBuilders).ToArray();
    }

    /// <summary>
    /// Builds the middleware pipeline for <paramref name="messageHandler"/>: asks each registered
    /// <see cref="IHandlerMiddlewareBuilder"/> to contribute middleware (skipping any that return
    /// <c>null</c> or are themselves <c>null</c>), then appends the handler invocation as the final step.
    /// The per-builder-set structure is cached and reused across messages; the middleware and handler
    /// instances are still resolved per invocation from the per-call resolver.
    /// </summary>
    /// <typeparam name="TRequest">The strongly-typed request handled by the pipeline.</typeparam>
    /// <typeparam name="TResponse">The strongly-typed response produced by the pipeline.</typeparam>
    /// <param name="messageHandler">The handler to invoke at the end of the pipeline.</param>
    /// <param name="serviceResolver">Resolver passed to each <see cref="IHandlerMiddlewareBuilder"/> when the chain runs.</param>
    /// <returns>The assembled pipeline.</returns>
    public IMiddlewarePipeline<IMessageHandlerContext<TRequest, TResponse>> Create<TRequest, TResponse>(
        IMessageHandler<TRequest, TResponse> messageHandler, IServiceResolver serviceResolver)
        where TRequest : class
    {
        var structure = HandlerPipelineStructureCache<TRequest, TResponse>.GetOrAdd(_routerMiddlewareBuilders);
        return new HandlerMiddlewarePipeline<TRequest, TResponse>(structure, messageHandler);
    }
}
