using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;

namespace Benzene.Core.MessageHandlers;

/// <summary>
/// No-op <see cref="IMessageHandlerResultSetter{TContext}"/> base class for transports that don't
/// need to report a handler's outcome back onto the context at all (e.g. because the transport's own
/// trigger/consumer handles acknowledgement automatically, regardless of success or failure).
/// </summary>
/// <typeparam name="TContext">The transport context type.</typeparam>
/// <remarks>
/// The "Message" + "MessageHandlerResultSetter" naming reflects what the type does, not a typo: like
/// <see cref="MessageMessageHandlerResultSetterBase{TContext}"/> and
/// <see cref="ResponseMessageMessageHandlerResultSetterBase{TContext}"/>, it implements
/// <c>IMessageHandlerResultSetter&lt;TContext&gt;</c>, but deliberately discards the result instead
/// of recording it or writing a response - use this as the base for a transport where there is
/// nothing meaningful to set.
/// </remarks>
public abstract class DefaultMessageMessageHandlerResultSetterBase<TContext>: IMessageHandlerResultSetter<TContext>
{
    /// <summary>
    /// Does nothing with <paramref name="messageHandlerResult"/>; provided for transports that have
    /// no way (or need) to report a handler's outcome back onto <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The transport context (unused).</param>
    /// <param name="messageHandlerResult">The outcome of routing and handling the message (unused).</param>
    /// <returns>A completed task.</returns>
    public Task SetResultAsync(TContext context, IMessageHandlerResult messageHandlerResult)
    {
        return Task.CompletedTask;
    }
}
