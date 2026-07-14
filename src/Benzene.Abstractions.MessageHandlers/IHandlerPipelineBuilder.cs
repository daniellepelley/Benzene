using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Builds the per-handler middleware pipeline (<see cref="IMiddlewarePipeline{TContext}"/> over
/// <see cref="IMessageHandlerContext{TRequest, TResponse}"/>) that wraps a message handler with the
/// middleware contributed by every registered <see cref="IHandlerMiddlewareBuilder"/>, finishing with
/// the handler invocation itself. Used by an <c>IMessageHandlerWrapper</c> implementation when a
/// handler is first created, so the same pipeline runs on every subsequent call to that handler.
/// </summary>
public interface IHandlerPipelineBuilder
{
    /// <summary>
    /// Registers additional <see cref="IHandlerMiddlewareBuilder"/> instances to be included in every
    /// pipeline created by this builder from now on.
    /// </summary>
    /// <param name="routerMiddlewareBuilders">The middleware builders to add.</param>
    void Add(params IHandlerMiddlewareBuilder[] routerMiddlewareBuilders);

    /// <summary>
    /// Builds the middleware pipeline for a specific handler by asking every registered
    /// <see cref="IHandlerMiddlewareBuilder"/> to contribute middleware, then appending the handler
    /// invocation itself as the final step.
    /// </summary>
    /// <typeparam name="TRequest">The strongly-typed request handled by the pipeline.</typeparam>
    /// <typeparam name="TResponse">The strongly-typed response produced by the pipeline.</typeparam>
    /// <param name="messageHandler">The handler to invoke at the end of the pipeline.</param>
    /// <param name="serviceResolver">Resolver passed to each <see cref="IHandlerMiddlewareBuilder"/>.</param>
    /// <returns>The assembled pipeline, ready to run for each invocation of <paramref name="messageHandler"/>.</returns>
    IMiddlewarePipeline<IMessageHandlerContext<TRequest, TResponse>> Create<TRequest, TResponse>(
        IMessageHandler<TRequest, TResponse> messageHandler,
        IServiceResolver serviceResolver)
        where TRequest : class;
}
