using Benzene.Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Benzene.Azure.AspNet;

public static class Extensions
{
    public static void EnsureResponseExists(this AspNetContext context)
    {
        context.ContentResult ??= new ContentResult();
    }

    // public static IMiddlewarePipelineBuilder<AspNetContext> UseHealthCheck(this IMiddlewarePipelineBuilder<AspNetContext> app, string topic, string method, string path, params IHealthCheck[] healthChecks)
    // {
    //     return app.Use(_ => new FuncWrapperMiddleware<AspNetContext>("HealthCheck", async (context, next) =>
    //     {
    //         if (string.Equals(context.HttpRequest.Method, method, StringComparison.InvariantCultureIgnoreCase) &&
    //             context.HttpRequest.Path == path)
    //         {
    //             context.MessageResult = await HealthCheckProcessor.PerformHealthChecksAsync(topic, healthChecks);
    //         }
    //         else
    //         {
    //             await next();
    //         }
    //     }));
    // }
    
    public static Task<IActionResult> HandleHttpRequest(this IAzureFunctionApp source, HttpRequest httpRequest)
    {
        return source.HandleAsync<HttpRequest, IActionResult>(httpRequest);
    }
}
