using Benzene.Abstractions.Messages;

namespace Benzene.Abstractions.MessageHandlers;

/// <summary>
/// Wraps an application-authored handler (either request/response or no-response) into a
/// <see cref="IMessageHandler{TRequest, TResponse}"/>, applying whatever cross-cutting behavior the
/// implementation provides (e.g. running it through an <see cref="IHandlerPipelineBuilder"/> pipeline).
/// Called once per handler by <see cref="IMessageHandlerFactory"/> when a handler is first resolved,
/// so the resulting wrapped instance can be reused for subsequent invocations of that handler.
/// </summary>
public interface IMessageHandlerWrapper
{
    /// <summary>Wraps a no-response handler so it can be invoked as an <see cref="IMessageHandler{TRequest, TResponse}"/>.</summary>
    /// <typeparam name="TRequest">The strongly-typed request the handler accepts.</typeparam>
    /// <typeparam name="TResponse">The response type to present the wrapped handler as (the inner handler never actually produces one).</typeparam>
    /// <param name="topic">The topic this handler is registered for.</param>
    /// <param name="messageHandler">The no-response handler to wrap.</param>
    /// <returns>A request/response handler that delegates to <paramref name="messageHandler"/>.</returns>
    IMessageHandler<TRequest, TResponse> Wrap<TRequest, TResponse>(ITopic topic, IMessageHandler<TRequest> messageHandler)
        where TRequest : class;

    /// <summary>Wraps a request/response handler, applying this wrapper's cross-cutting behavior.</summary>
    /// <typeparam name="TRequest">The strongly-typed request the handler accepts.</typeparam>
    /// <typeparam name="TResponse">The strongly-typed response the handler returns.</typeparam>
    /// <param name="topic">The topic this handler is registered for.</param>
    /// <param name="messageHandler">The handler to wrap.</param>
    /// <returns>The wrapped handler.</returns>
    IMessageHandler<TRequest, TResponse> Wrap<TRequest, TResponse>(ITopic topic, IMessageHandler<TRequest, TResponse> messageHandler)
        where TRequest : class;
}
