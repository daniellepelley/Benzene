using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Http.Routing;

namespace Benzene.Http.Cors;

public static class Extensions
{
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