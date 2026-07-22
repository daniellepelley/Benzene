using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Middleware;
using Benzene.Clients;
using Benzene.Core.Middleware;
using Benzene.HealthChecks.Core;
using Microsoft.Extensions.Logging;

namespace Benzene.Client.Http;

public static class Extensions
{
    /// <summary>
    /// Registers a scoped <see cref="HttpBenzeneMessageClient"/> (as itself and as
    /// <see cref="IBenzeneMessageClient"/>) that POSTs the BenzeneMessage envelope to <paramref name="url"/>
    /// — the target service's BenzeneMessage endpoint. Resolves the <see cref="HttpClient"/> from DI (you
    /// must register one, e.g. via <c>AddHttpClient()</c>/<c>AddSingleton&lt;HttpClient&gt;()</c>), along with
    /// an optional logger and the ambient cancellation-token accessor.
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="url">The target BenzeneMessage endpoint URL (verbatim request URI; absolute or relative to the client's <c>BaseAddress</c>).</param>
    /// <param name="healthCheck">
    /// When <c>true</c> (the default) a non-destructive reachability check for <paramref name="url"/> is
    /// auto-registered on the deep <c>healthcheck</c> layer (never a Kubernetes probe — see
    /// <see cref="IDependencyHealthCheck"/>). Pass <c>false</c> to opt out. Deduped by <c>"HttpBenzeneMessage:{url}"</c>.
    /// </param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddHttpBenzeneMessageClient(this IBenzeneServiceContainer services, string url, bool healthCheck = true)
    {
        services.AddScoped<IBenzeneMessageClient>(resolver => new HttpBenzeneMessageClient(
            resolver.GetService<HttpClient>(), url,
            resolver.TryGetService<ILogger<HttpBenzeneMessageClient>>(),
            resolver.TryGetService<ICancellationTokenAccessor>()));

        if (healthCheck)
        {
            services.AddDependencyHealthCheck(
                resolver => new HttpBenzeneMessageHealthCheck(resolver.GetService<HttpClient>(), url,
                    cancellation: resolver.TryGetService<ICancellationTokenAccessor>()),
                $"HttpBenzeneMessage:{url}");
        }

        return services;
    }

    /// <summary>
    /// Adds an explicit <see cref="HttpBenzeneMessageHealthCheck"/> for <paramref name="url"/> — a
    /// non-destructive POST of a <paramref name="healthCheckTopic"/> envelope to the target's BenzeneMessage
    /// endpoint. Use this to health-check a target you call through some other wiring; the
    /// <c>AddHttpBenzeneMessageClient</c> default already auto-wires one.
    /// </summary>
    /// <param name="builder">The health check builder to add the check to.</param>
    /// <param name="url">The target BenzeneMessage endpoint URL to probe.</param>
    /// <param name="healthCheckTopic">The topic to POST (defaults to <c>"healthcheck"</c>).</param>
    /// <returns>The health check builder, for chaining.</returns>
    public static IHealthCheckBuilder AddHttpBenzeneMessageHealthCheck(this IHealthCheckBuilder builder, string url, string healthCheckTopic = "healthcheck")
    {
        return builder.AddHealthCheck(resolver => new HttpBenzeneMessageHealthCheck(
            resolver.GetService<HttpClient>(), url, healthCheckTopic, resolver.TryGetService<ICancellationTokenAccessor>()));
    }

    public static IMiddlewarePipelineBuilder<HttpSendMessageContext> UseHttpClient(
        this IMiddlewarePipelineBuilder<HttpSendMessageContext> app, HttpClient httpClient)
    {
        return app.Use(_ => new HttpClientMiddleware(httpClient));
    }
 
    public static IMiddlewarePipelineBuilder<HttpSendMessageContext> UseHttpClient(
        this IMiddlewarePipelineBuilder<HttpSendMessageContext> app)
    {
        app.Register(x => x.AddScoped<HttpClientMiddleware>());
        return app.Use<HttpSendMessageContext, HttpClientMiddleware>();
    }
   
     public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, IMiddlewarePipeline<TContextOut> middlewarePipeline)
    {
        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }

    public static IMiddlewarePipelineBuilder<TContext> Convert<TContext, TContextOut>(this IMiddlewarePipelineBuilder<TContext> app,
        IContextConverter<TContext, TContextOut> converter, Action<IMiddlewarePipelineBuilder<TContextOut>> action)
    {
        var middlewarePipeline = app.CreateMiddlewarePipeline(action);

        return app.Use(serviceResolver => new ContextConverterMiddleware<TContext, TContextOut>(converter, middlewarePipeline, serviceResolver));
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>> UseHttp<TRequest, TResponse>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>> app,
        string verb, string path, Action<IMiddlewarePipelineBuilder<HttpSendMessageContext>> action)
    {
        return Convert(app, new HttpContextConverter<TRequest, TResponse>(verb, path), action);
    }
    
    public static IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>> UseHttp<TRequest, TResponse>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<TRequest, TResponse>> app,
        string verb, string path)
    {
        return app.Convert(new HttpContextConverter<TRequest, TResponse>(verb, path), builder => builder.UseHttpClient());
    }
}