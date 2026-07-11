using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Http.Routing;

namespace Benzene.Http.Cors;

/// <summary>
/// Provides extension methods for configuring CORS middleware in the Benzene pipeline.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds CORS middleware to the pipeline with the specified settings.
    /// </summary>
    /// <typeparam name="TContext">The HTTP context type.</typeparam>
    /// <param name="app">The middleware pipeline builder.</param>
    /// <param name="corsSettings">The CORS configuration settings specifying allowed domains and headers.</param>
    /// <returns>The middleware pipeline builder for method chaining.</returns>
    /// <remarks>
    /// This middleware should be added early in the pipeline to ensure CORS headers are
    /// applied to all responses. It handles preflight OPTIONS requests automatically and
    /// validates Origin headers against the configured allowed domains.
    /// </remarks>
    public static IMiddlewarePipelineBuilder<TContext> UseCors<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, CorsSettings corsSettings) where TContext : IHttpContext
    {
        app.Register(x =>
            x.AddSingleton(resolver => new CorsMiddleware<TContext>(
                corsSettings, resolver.GetService<IHttpEndpointFinder>(),
                resolver.GetService<IHttpRequestAdapter<TContext>>(),
                resolver.GetService<IBenzeneResponseAdapter<TContext>>(),
                resolver.GetService<IMessageHandlerResultSetter<TContext>>()
            )));
            
        return app.Use<TContext, CorsMiddleware<TContext>>();
    }
}