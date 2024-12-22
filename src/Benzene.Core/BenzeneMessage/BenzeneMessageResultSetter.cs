using Benzene.Abstractions.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Core.BenzeneMessage;

public class BenzeneMessageResultSetter : ResponseMessageResultSetterBase<BenzeneMessageContext>
{
    public BenzeneMessageResultSetter(IResponseHandlerContainer<BenzeneMessageContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}