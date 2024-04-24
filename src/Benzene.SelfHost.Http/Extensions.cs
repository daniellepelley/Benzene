using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Core.DI;
using Benzene.HostedService;

namespace Benzene.SelfHost.Http;

public static class Extensions
{
    public static IHostedServiceAppBuilder UseHttp(this IHostedServiceAppBuilder app, BenzeneHttpConfig benzeneHttpConfig, Action<IMiddlewarePipelineBuilder<HttpContext>> action)
    {
        app.Register(x => x.AddHttp());
        var middlewarePipelineBuilder = app.Create<HttpContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        
        var httpApplication = new HttpApplication(pipeline);
        app.Add(serviceResolverFactory => new BenzeneHttpConsumer(serviceResolverFactory, httpApplication, benzeneHttpConfig));
        return app;
    }
}
