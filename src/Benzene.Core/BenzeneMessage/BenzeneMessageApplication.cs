using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.Core.BenzeneMessage;

public class BenzeneMessageApplication : MiddlewareApplication<IBenzeneMessageRequest, BenzeneMessageContext, IBenzeneMessageResponse>
{
    public BenzeneMessageApplication(IMiddlewarePipeline<BenzeneMessageContext> pipeline)
        : base(pipeline, 
            @event => new BenzeneMessageContext(@event),
            context => context.BenzeneMessageResponse)
    { }
}
