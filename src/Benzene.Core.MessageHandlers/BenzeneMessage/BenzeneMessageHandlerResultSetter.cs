using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// The <c>BenzeneMessage</c> transport's <see cref="IMessageHandlerResultSetter{TContext}"/>: writes
/// the handler's result as a <see cref="BenzeneMessageContext"/> response by running the registered
/// response handlers (headers, status, serialized body), like
/// <see cref="ResponseMessageHandlerResultSetterBase{TContext}"/> does - unless
/// <see cref="BenzeneMessageResponseSuppression.IsSuppressed"/> is set for this message, in which case
/// it does nothing (a one-way host discards the response, so serializing it is wasted work).
/// Registered by <c>AddBenzeneMessage</c>.
/// </summary>
public class BenzeneMessageHandlerResultSetter : IMessageHandlerResultSetter<BenzeneMessageContext>
{
    private readonly IResponseHandlerContainer<BenzeneMessageContext> _responseHandlerContainer;
    private readonly BenzeneMessageResponseSuppression _suppression;

    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneMessageHandlerResultSetter"/> class.
    /// </summary>
    /// <param name="responseHandlerContainer">Runs the registered response handlers to produce the outbound response.</param>
    /// <param name="suppression">The current message's scoped response-suppression flag (set on one-way hosts).</param>
    public BenzeneMessageHandlerResultSetter(IResponseHandlerContainer<BenzeneMessageContext> responseHandlerContainer,
        BenzeneMessageResponseSuppression suppression)
    {
        _responseHandlerContainer = responseHandlerContainer;
        _suppression = suppression;
    }

    /// <summary>
    /// Writes the response via the response handlers, or does nothing when the response is suppressed
    /// for this message.
    /// </summary>
    /// <param name="context">The BenzeneMessage context to write the response onto.</param>
    /// <param name="messageHandlerResult">The outcome of routing and handling the message.</param>
    public async Task SetResultAsync(BenzeneMessageContext context, IMessageHandlerResult messageHandlerResult)
    {
        if (_suppression.IsSuppressed)
        {
            return;
        }

        await _responseHandlerContainer.HandleAsync(context, messageHandlerResult);
    }
}
