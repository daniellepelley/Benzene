using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Core.BenzeneMessage;

public class BenzeneMessageMessageHandlerResultSetter : ResponseMessageMessageHandlerResultSetterBase<BenzeneMessageContext>
{
    public BenzeneMessageMessageHandlerResultSetter(IResponseHandlerContainer<BenzeneMessageContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}