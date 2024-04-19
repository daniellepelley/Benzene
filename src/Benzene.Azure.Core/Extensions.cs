using Azure.Messaging.EventHubs;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Azure.Core.AspNet;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace Benzene.Azure.Core;

public static class Extensions
{
    public static Task<IActionResult> HandleHttpRequest(this IAzureApp source, HttpRequest httpRequest)
    {
        return source.HandleAsync<HttpRequest, IActionResult>(httpRequest);
    }
    
    public static Task HandleEventHub(this IAzureApp source, params EventData[] eventData)
    {
        return source.HandleAsync(eventData);
    }
    
    public static Task HandleKafkaEvents(this IAzureApp source, params KafkaEventData<string>[] eventData)
    {
        return source.HandleAsync(eventData);
    }


    public static IMiddlewarePipelineBuilder<AspNetContext> UseHealthCheck(this IMiddlewarePipelineBuilder<AspNetContext> app, string topic, string method, string path, params IHealthCheck[] healthChecks)
    {
        return app.Use(_ => new FuncWrapperMiddleware<AspNetContext>("HealthCheck", async (context, next) =>
        {
            if (string.Equals(context.HttpRequest.Method, method, StringComparison.InvariantCultureIgnoreCase) &&
                context.HttpRequest.Path == path)
            {
                context.MessageResult = await HealthCheckProcessor.PerformHealthChecksAsync(topic, healthChecks);
            }
            else
            {
                await next();
            }
        }));
    }
}
