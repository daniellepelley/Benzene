using Benzene.Abstractions.DI;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// The entry point application registered into the ASP.NET Core request pipeline by
/// <see cref="AspApplicationBuilder"/>. Wraps the incoming <see cref="HttpContext"/> in an
/// <see cref="AspNetContext"/> and runs it through the middleware pipeline.
/// </summary>
public class AspNetApplication : EntryPointMiddlewareApplication<HttpContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetApplication"/> class.
    /// </summary>
    /// <param name="pipelineBuilder">The built HTTP middleware pipeline to run the request through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process the request.</param>
    public AspNetApplication(IMiddlewarePipeline<AspNetContext> pipelineBuilder, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareApplication<HttpContext, AspNetContext>(pipelineBuilder,
                @event => new AspNetContext(@event)),
            serviceResolverFactory)
    { }
}
