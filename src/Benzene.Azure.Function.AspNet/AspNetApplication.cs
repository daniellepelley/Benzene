using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.Info;
using Benzene.Core.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Benzene.Azure.Function.AspNet;

/// <summary>
/// The entry point application for an HTTP-triggered Azure Function. Wraps the incoming
/// <see cref="HttpRequest"/> in an <see cref="AspNetContext"/>, runs it through the middleware pipeline,
/// and returns the resulting <see cref="IActionResult"/>.
/// </summary>
public class AspNetApplication : EntryPointMiddlewareApplication<HttpRequest, IActionResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetApplication"/> class.
    /// </summary>
    /// <param name="pipelineBuilder">The built HTTP middleware pipeline to run the request through.</param>
    /// <param name="serviceResolverFactory">The service resolver factory used to process the request.</param>
    public AspNetApplication(IMiddlewarePipeline<AspNetContext> pipelineBuilder, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareApplication<HttpRequest, AspNetContext, IActionResult>(
                new TransportMiddlewarePipeline<AspNetContext>(TransportNames.Asp, pipelineBuilder),
                @event => new AspNetContext(@event),
                    context => context.ContentResult
                ),
            serviceResolverFactory)
    { }
}
