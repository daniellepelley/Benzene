using Benzene.Abstractions.Hosting;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Microsoft.AspNetCore.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// Provides the ASP.NET Core implementation of <see cref="IBenzeneInvocation"/>.
/// </summary>
public static class BenzeneInvocationExtensions
{
    /// <summary>The platform identifier reported by <see cref="IBenzeneInvocation.Platform"/> on ASP.NET Core.</summary>
    public const string PlatformName = "AspNet";

    /// <summary>
    /// Adds middleware that exposes an <see cref="IBenzeneInvocation"/> for the duration of the request,
    /// with <see cref="IBenzeneInvocation.InvocationId"/> set to the request's trace identifier and
    /// <c>GetFeature&lt;HttpContext&gt;()</c> returning the native ASP.NET Core <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="app">The pipeline builder to add the invocation middleware to.</param>
    /// <returns>The pipeline builder, for method chaining.</returns>
    public static IMiddlewarePipelineBuilder<AspNetContext> UseBenzeneInvocation(
        this IMiddlewarePipelineBuilder<AspNetContext> app)
    {
        return app.UseBenzeneInvocation((_, context) => new BenzeneInvocation(
            context.HttpContext.TraceIdentifier,
            PlatformName,
            new Dictionary<Type, object> { [typeof(HttpContext)] = context.HttpContext }));
    }
}
