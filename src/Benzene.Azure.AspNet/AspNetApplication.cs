using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Benzene.Azure.AspNet;

public class AspNetApplication : EntryPointMiddlewareApplication<HttpRequest, IActionResult>
{
    public AspNetApplication(IMiddlewarePipeline<AspNetContext> pipelineBuilder, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareApplication<HttpRequest, AspNetContext, IActionResult>(pipelineBuilder,
                @event => new AspNetContext(@event),
                    context => context.ContentResult
                ),
            serviceResolverFactory)
    { }
}
