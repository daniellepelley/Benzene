using System.Net;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.SelfHost.Http;

public class HttpListenerApplication : MiddlewareApplication<HttpListenerContext, SelfHostHttpContext>
{
    public HttpListenerApplication(IMiddlewarePipeline<SelfHostHttpContext> pipeline)
        : base(pipeline,
            @event => new SelfHostHttpContext(@event))
    { }
}
