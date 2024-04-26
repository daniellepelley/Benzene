using Benzene.Abstractions.MiddlewareBuilder;

namespace Benzene.SelfHost.Http;

public static class Extensions
{
    public static IBenzeneWorkerBuilder UseHttp(this IBenzeneWorkerBuilder app, BenzeneHttpConfig benzeneHttpConfig, Action<IMiddlewarePipelineBuilder<HttpContext>> action)
    {
        app.Register(x => x.AddHttp());
        var middlewarePipelineBuilder = app.Create<HttpContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();
        
        var httpApplication = new HttpApplication(pipeline);
        app.Add(serviceResolverFactory => new BenzeneHttpWorker(serviceResolverFactory, httpApplication, benzeneHttpConfig));
        return app;
    }
}
