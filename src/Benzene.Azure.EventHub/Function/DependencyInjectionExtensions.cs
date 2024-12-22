using Benzene.Abstractions.Middleware;
using Benzene.Azure.Core;

namespace Benzene.Azure.EventHub.Function;

public static class DependencyInjectionExtensions
{
    public static IAzureFunctionAppBuilder UseEventHub(this IAzureFunctionAppBuilder app, Action<IMiddlewarePipelineBuilder<EventHubContext>> action)
    {
        var pipeline = app.Create<EventHubContext>();
        action(pipeline);
        app.Add(serviceResolverFactory => new EventHubApplication(pipeline.Build(), serviceResolverFactory));
        return app;
    }

}
