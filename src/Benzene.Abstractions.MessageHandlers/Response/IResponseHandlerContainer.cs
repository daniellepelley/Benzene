using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Abstractions.MessageHandlers.Response;

/// <summary>
/// Runs every registered <see cref="IResponseHandler{TContext}"/> (sync and async) against a
/// handler's result to build the outgoing response, then finalizes it via
/// <see cref="IBenzeneResponseAdapter{TContext}"/>. Typically used from an
/// <see cref="IMessageHandlerResultSetter{TContext}"/> for transports that write an explicit
/// response (e.g. HTTP), as opposed to fire-and-forget transports that don't need one.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the response is written to.</typeparam>
public interface IResponseHandlerContainer<TContext>
{
    /// <summary>Runs all registered response handlers for the given result, then finalizes the response.</summary>
    /// <param name="context">The transport-specific context to write the response to.</param>
    /// <param name="messageHandlerResult">The outcome of routing and invoking the handler.</param>
    Task HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}