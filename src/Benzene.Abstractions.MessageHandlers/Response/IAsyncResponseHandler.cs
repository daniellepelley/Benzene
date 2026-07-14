namespace Benzene.Abstractions.MessageHandlers.Response;

/// <summary>
/// A response handler whose work is asynchronous (e.g. writing to a network stream). Implementations
/// are discovered as <see cref="IResponseHandler{TContext}"/> and, along with
/// <see cref="ISyncResponseHandler{TContext}"/> implementations, composed by an
/// <see cref="IResponseHandlerContainer{TContext}"/> to process a handler's result.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public interface IAsyncResponseHandler<TContext> : IResponseHandler<TContext>
{
    /// <summary>Asynchronously processes the handler's result against the transport context.</summary>
    /// <param name="context">The transport-specific context to write the response to.</param>
    /// <param name="messageHandlerResult">The outcome of routing and invoking the handler.</param>
    Task HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}
