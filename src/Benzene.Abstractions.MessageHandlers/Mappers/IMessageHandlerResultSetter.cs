namespace Benzene.Abstractions.MessageHandlers.Mappers;

/// <summary>
/// Writes a handler's outcome back out through the transport, e.g. by producing an HTTP response
/// body/status, writing to <see cref="IHasMessageResult"/> for a transport that reports completion
/// via its own trigger mechanism (see <see cref="IHasMessageResult"/> for details), or doing nothing
/// for fire-and-forget transports. Despite the "Setter" name, implementations are expected to perform
/// the transport-specific side effect of delivering the result, not just assign a property -- treat
/// it as "apply this result to the transport", not a plain property setter.
/// </summary>
/// <typeparam name="TContext">The transport-specific context type the result is written to.</typeparam>
public interface IMessageHandlerResultSetter<TContext>
{
    /// <summary>Applies the handler's result to the given transport context.</summary>
    /// <param name="context">The transport-specific context for the message being handled.</param>
    /// <param name="messageHandlerResult">The outcome of routing and invoking the handler.</param>
    Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult);
}