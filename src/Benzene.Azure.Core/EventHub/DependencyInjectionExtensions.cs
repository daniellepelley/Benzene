using Benzene.Abstractions.MiddlewareBuilder;

namespace Benzene.Azure.Core.EventHub;

public static class DependencyInjectionExtensions
{
    public static IAzureAppBuilder UseEventHub(this IAzureAppBuilder app, Action<IMiddlewarePipelineBuilder<EventHubContext>> action)
    {
        var pipeline = app.Create<EventHubContext>();
        action(pipeline);
        app.Add(serviceResolverFactory => new EventHubApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }

}
