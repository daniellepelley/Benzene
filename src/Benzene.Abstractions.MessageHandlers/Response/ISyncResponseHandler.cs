namespace Benzene.Abstractions.MessageHandlers.Response;

/// <summary>
/// A response handler whose work is synchronous (e.g. setting a status code or header).
/// Implementations are discovered as <see cref="IResponseHandler{TContext}"/> and, along with
/// <see cref="IAsyncResponseHandler{TContext}"/> implementations, composed by an
/// <see cref="IResponseHandlerContainer{TContext}"/> to process a handler's result.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public interface ISyncResponseHandler<TContext> : IResponseHandler<TContext>
{
    /// <summary>Synchronously processes the handler's result against the transport context.</summary>
    /// <param name="context">The transport-specific context to write the response to.</param>
    /// <param name="messageHandlerResult">The outcome of routing and invoking the handler.</param>
    void HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}
