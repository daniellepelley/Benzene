using Benzene.Abstractions.Response;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Response;

namespace Benzene.AspNet.Core;

public class AspMessageResultSetter : ResponseIfHandledResultSetter<AspNetContext>
{
    public AspMessageResultSetter(IResponseHandlerContainer<AspNetContext> responseHandlerContainer) : base(responseHandlerContainer)
    {
    }
}