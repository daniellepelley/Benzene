using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.AspNet;

public class AspNetMessageMessageHandlerResultSetter : ResponseMessageMessageHandlerResultSetterBase<AspNetContext>
{
    public AspNetMessageMessageHandlerResultSetter(IResponseHandlerContainer<AspNetContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}