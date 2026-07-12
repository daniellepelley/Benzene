using Benzene.Azure.Function.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Benzene.Azure.Function.AspNet;

/// <summary>
/// Provides extension methods for preparing an <see cref="AspNetContext"/>'s response and dispatching
/// HTTP trigger requests to a built <see cref="IAzureFunctionApp"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Ensures the context has a <see cref="ContentResult"/> to write to, creating an empty one if needed.
    /// </summary>
    /// <param name="context">The context to ensure a response exists on.</param>
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

    /// <summary>
    /// Dispatches an HTTP trigger request to the Azure Function app's HTTP entry point application.
    /// </summary>
    /// <param name="source">The built Azure Function app to dispatch to.</param>
    /// <param name="httpRequest">The incoming HTTP request.</param>
    /// <returns>A task that resolves to the action result to return from the trigger function.</returns>
    public static Task<IActionResult> HandleHttpRequest(this IAzureFunctionApp source, HttpRequest httpRequest)
    {
        return source.HandleAsync<HttpRequest, IActionResult>(httpRequest);
    }
}
