using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Core.DirectMessage;

public class DirectMessageApplication : MiddlewareApplication<IDirectMessageRequest, DirectMessageContext, IDirectMessageResponse>
{
    public DirectMessageApplication(IMiddlewarePipeline<DirectMessageContext> pipeline)
        : base(pipeline, 
            @event => DirectMessageContext.CreateInstance(@event),
            context => context.DirectMessageResponse)
    { }
}
