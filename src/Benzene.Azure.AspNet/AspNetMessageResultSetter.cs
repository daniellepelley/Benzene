using Benzene.Abstractions.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Azure.AspNet;

public class AspNetMessageResultSetter : ResponseMessageResultSetterBase<AspNetContext>
{
    public AspNetMessageResultSetter(IResponseHandlerContainer<AspNetContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}