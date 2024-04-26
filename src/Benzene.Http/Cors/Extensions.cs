using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Abstractions.Response;
using Benzene.Abstractions.Results;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Http.Routing;

namespace Benzene.Http.Cors
{
    public static class Extensions
    {
        public static IMiddlewarePipelineBuilder<TContext> UseCors<TContext>(
            this IMiddlewarePipelineBuilder<TContext> app, CorsSettings corsSettings) where TContext : IHasMessageResult, IHttpContext
        {
            app.Register(x =>
                x.AddSingleton(resolver => new CorsMiddleware<TContext>(
                corsSettings, resolver.GetService<IHttpEndpointFinder>(),
                resolver.GetService<IHttpRequestAdapter<TContext>>(),
                resolver.GetService<IBenzeneResponseAdapter<TContext>>()
                )));
            
            return app.Use<TContext, CorsMiddleware<TContext>>();
        }
    }
}
