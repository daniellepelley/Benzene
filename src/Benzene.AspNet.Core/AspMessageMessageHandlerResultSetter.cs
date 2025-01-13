using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Response;

namespace Benzene.AspNet.Core;

public class AspMessageMessageHandlerResultSetter : ResponseIfHandledMessageHandlerResultSetter<AspNetContext>
{
    public AspMessageMessageHandlerResultSetter(IResponseHandlerContainer<AspNetContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}