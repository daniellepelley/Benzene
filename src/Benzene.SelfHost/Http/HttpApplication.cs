using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;

namespace Benzene.SelfHost.Http;

public class HttpApplication : MiddlewareApplication<HttpRequest, HttpContext, HttpResponse>
{
    public HttpApplication(IMiddlewarePipeline<HttpContext> pipeline)
        : base(pipeline,
            @event => new HttpContext(@event),
            context => context.Response)
    { }
}
