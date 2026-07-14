using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Factory for a single piece of handler middleware, registered with an <see cref="IMessageRouterBuilder"/>
/// so <see cref="IHandlerPipelineBuilder"/> can include it in every handler pipeline it builds
/// (e.g. cross-cutting concerns such as validation, logging enrichment, or filters).
/// </summary>
public interface IHandlerMiddlewareBuilder
{
    /// <summary>
    /// Creates the middleware instance for a specific handler invocation, or <c>null</c> if this
    /// builder has nothing to contribute for the given <typeparamref name="TRequest"/>/<typeparamref name="TResponse"/>
    /// pair (in which case <see cref="IHandlerPipelineBuilder"/> skips it).
    /// </summary>
    /// <typeparam name="TRequest">The strongly-typed request handled by the pipeline being built.</typeparam>
    /// <typeparam name="TResponse">The strongly-typed response produced by the pipeline being built.</typeparam>
    /// <param name="serviceResolver">Resolver used to pull any dependencies the middleware needs.</param>
    /// <param name="messageHandler">The handler this pipeline is being built for.</param>
    /// <returns>The middleware to add to the pipeline, or <c>null</c> to contribute nothing.</returns>
    IMiddleware<IMessageHandlerContext<TRequest, TResponse>>? Create<TRequest, TResponse>(IServiceResolver serviceResolver, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class;
}
