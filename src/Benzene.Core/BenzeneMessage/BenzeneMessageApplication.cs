using Benzene.Abstractions.Middleware;
using Benzene.Core.Info;
using Benzene.Core.Middleware;

namespace Benzene.Core.BenzeneMessage;

public class BenzeneMessageApplication : MiddlewareApplication<IBenzeneMessageRequest, BenzeneMessageContext, IBenzeneMessageResponse>
{
    public BenzeneMessageApplication(IMiddlewarePipeline<BenzeneMessageContext> pipeline)
        : base(
            new TransportMiddlewarePipeline<BenzeneMessageContext>("benzene", pipeline),
            @event => new BenzeneMessageContext(@event),
            context => context.BenzeneMessageResponse)
    { }
}
