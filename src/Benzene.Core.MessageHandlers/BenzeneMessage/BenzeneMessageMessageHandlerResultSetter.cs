using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Messages.BenzeneMessage;

namespace Benzene.Core.MessageHandlers.BenzeneMessage;

public class BenzeneMessageMessageHandlerResultSetter : ResponseMessageMessageHandlerResultSetterBase<BenzeneMessageContext>
{
    public BenzeneMessageMessageHandlerResultSetter(IResponseHandlerContainer<BenzeneMessageContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}