using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;

namespace Benzene.SelfHost.Http;

public class HttpListenerApplication : MiddlewareApplication<System.Net.HttpListenerContext, SelfHostHttpContext>
{
    public HttpListenerApplication(IMiddlewarePipeline<SelfHostHttpContext> pipeline)
        : base(new TransportMiddlewarePipeline<SelfHostHttpContext>(TransportNames.Http, pipeline),
            @event => new SelfHostHttpContext(@event))
    { }
}
