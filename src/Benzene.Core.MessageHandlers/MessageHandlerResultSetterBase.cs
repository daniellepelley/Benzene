using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// Base <see cref="IMessageHandlerResultSetter{TContext}"/> implementation for transports whose
/// context carries its own simple pass/fail completion outcome via <see cref="IHasMessageResult.MessageResult"/>
/// (e.g. so a queue/trigger-based transport can decide whether to acknowledge or retry the message),
/// rather than writing a response body back through <see cref="Benzene.Abstractions.MessageHandlers.Response.IBenzeneResponseAdapter{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">
/// The transport context type, which must implement <see cref="IHasMessageResult"/> so this setter
/// has somewhere to record the outcome.
/// </typeparam>
/// <remarks>
/// The "Message" + "MessageHandlerResultSetter" naming reflects what the type does, not a typo:
/// it is a setter that implements <c>IMessageHandlerResultSetter&lt;TContext&gt;</c> (the "MessageHandlerResultSetter"
/// part) by writing the handler's <see cref="Benzene.Abstractions.Results.IBenzeneResult"/> - the transport's own
/// completion/acknowledgement outcome (the "Message[Result]" part) - onto the context, rather than mapping the
/// handler's result into a response payload. See also <see cref="ResponseMessageHandlerResultSetterBase{TContext}"/>,
/// which does the response-writing equivalent for transports that produce a response body instead.
/// </remarks>
public abstract class MessageHandlerResultSetterBase<TContext>: IMessageHandlerResultSetter<TContext> where TContext : IHasMessageResult
{
    /// <summary>
    /// Records the handler's <see cref="Benzene.Abstractions.Results.IBenzeneResult"/> onto
    /// <see cref="IHasMessageResult.MessageResult"/>. Does not write a response body.
    /// </summary>
    /// <param name="context">The transport context to record the outcome on.</param>
    /// <param name="messageHandlerResult">The outcome of routing and handling the message.</param>
    /// <returns>A completed task; this setter's work is synchronous.</returns>
    public Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        context.MessageResult = messageHandlerResult.BenzeneResult;
        return Task.CompletedTask;
    }
}
