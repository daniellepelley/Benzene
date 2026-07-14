using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

/// <summary>
/// The <c>BenzeneMessage</c> transport's <see cref="Benzene.Abstractions.MessageHandlers.Mappers.IMessageHandlerResultSetter{TContext}"/>:
/// writes the handler's result as a <see cref="BenzeneMessageContext"/> response, via the shared
/// <see cref="ResponseMessageMessageHandlerResultSetterBase{TContext}"/> behavior (see that type for
/// why the "Message" appears twice in the name). Registered by <c>AddBenzeneMessage</c>.
/// </summary>
public class BenzeneMessageMessageHandlerResultSetter : ResponseMessageMessageHandlerResultSetterBase<BenzeneMessageContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BenzeneMessageMessageHandlerResultSetter"/> class.
    /// </summary>
    /// <param name="responseHandlerContainer">Runs the registered response handlers to produce the outbound response.</param>
    public BenzeneMessageMessageHandlerResultSetter(IResponseHandlerContainer<BenzeneMessageContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}
