using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

public class AspNetApplication : EntryPointMiddlewareApplication<HttpContext>
{
    public AspNetApplication(IMiddlewarePipeline<AspNetContext> pipelineBuilder, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareApplication<HttpContext, AspNetContext>(pipelineBuilder,
                @event => new AspNetContext(@event)),
            serviceResolverFactory)
    { }
}