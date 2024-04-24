using System.Net;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.SelfHost.Http;

public class HttpApplication : MiddlewareApplication<HttpListenerContext, HttpContext>
{
    public HttpApplication(IMiddlewarePipeline<HttpContext> pipeline)
        : base(pipeline,
            @event => new HttpContext(@event))
    { }
}
