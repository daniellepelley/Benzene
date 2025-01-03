using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.Response;

namespace Benzene.AspNet.Core;

public class AspMessageResultSetter : ResponseIfHandledResultSetter<AspNetContext>
{
    public AspMessageResultSetter(IResponseHandlerContainer<AspNetContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}