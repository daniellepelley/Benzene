using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Benzene.Http.RequestBody;
using Microsoft.Extensions.Logging;
using Constants = Benzene.HealthChecks.Constants;

namespace Benzene.SelfHost.Http;

public static class Extensions
{
    public static IBenzeneWorkerStartup UseHttp(this IBenzeneWorkerStartup app, BenzeneHttpConfig benzeneHttpConfig, Action<IMiddlewarePipelineBuilder<SelfHostHttpContext>> action)
    {
        // Register the config so request-scoped components (e.g. the body getter's size limit) can
        // resolve it, in addition to it being captured by the worker below.
        app.Register(x => x.AddHttp().AddSingleton<BenzeneHttpConfig>(_ => benzeneHttpConfig));
        var middlewarePipelineBuilder = app.Create<SelfHostHttpContext>();
        // Read the request body asynchronously, once, up front - so the synchronous
        // HttpListenerMessageBodyGetter serves it from memory instead of blocking a thread-pool thread.
        middlewarePipelineBuilder.UseBufferedRequestBody();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        var httpApplication = new HttpListenerApplication(pipeline);
        app.Add(serviceResolverFactory =>
        {
            using var scope = serviceResolverFactory.CreateScope();
            var logger = scope.GetService<ILogger<BenzeneHttpWorker>>();
            return new BenzeneHttpWorker(serviceResolverFactory, httpApplication, benzeneHttpConfig, logger);
        });
        return app;
    }
    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string method, string path,
        params IHealthCheck[] healthChecks)
            where TContext : IHttpContext
    {
        return app.UseHealthCheck(Constants.DefaultHealthCheckTopic, method, path, healthChecks);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string topic, string method, string path,
        params IHealthCheck[] healthChecks)
            where TContext : IHttpContext
    {
        return app.UseHealthCheck(topic, method, path, x => x.AddHealthChecks(healthChecks));
    }

    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(this IMiddlewarePipelineBuilder<TContext> app, string method, string path, Action<IHealthCheckBuilder> action)
        where TContext : IHttpContext
    {
        return app.UseHealthCheck(Constants.DefaultHealthCheckTopic, method, path, action);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(this IMiddlewarePipelineBuilder<TContext> app, string topic, string method, string path, Action<IHealthCheckBuilder> action)
        where TContext : IHttpContext
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);
        return app.UseHealthCheck(topic, method, path, builder);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(this IMiddlewarePipelineBuilder<TContext> app, string topic, string method, string path, IHealthCheckBuilder builder)
        where TContext : IHttpContext
    {
        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("HealthCheck", async (context, next) =>
        {
            var httpRequestAdapter = resolver.GetService<IHttpRequestAdapter<TContext>>();
            var resultSetter = resolver.GetService<IMessageHandlerResultSetter<TContext>>();
            var httpRequest = httpRequestAdapter.Map(context);
            if (httpRequest.Method.ToUpperInvariant() == method.ToUpperInvariant() &&
                httpRequest.Path == path)
            {
                var result = await HealthCheckProcessor.PerformHealthChecksAsync(topic, builder.GetHealthChecks(resolver));
                await resultSetter.SetResultAsync(context, new MessageHandlerResult(new Topic(topic), MessageHandlerDefinition.Empty(), result));
            }
            else
            {
                await next();
            }
        }));
    }

    /// <summary>
    /// Adds a Kubernetes-style liveness-check endpoint at <c>GET /livez</c>, using
    /// <see cref="Constants.DefaultLivenessTopic"/>. See
    /// <see cref="Benzene.HealthChecks.Extensions.UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>
    /// for the liveness/readiness distinction.
    /// </summary>
    public static IMiddlewarePipelineBuilder<TContext> UseLivenessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, params IHealthCheck[] healthChecks)
            where TContext : IHttpContext
    {
        return app.UseLivenessCheck("/livez", healthChecks);
    }

    /// <summary>Same as <see cref="UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>, at a custom <paramref name="path"/> instead of the conventional <c>/livez</c>.</summary>
    public static IMiddlewarePipelineBuilder<TContext> UseLivenessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string path, params IHealthCheck[] healthChecks)
            where TContext : IHttpContext
    {
        return app.UseLivenessCheck(path, x => x.AddHealthChecks(healthChecks));
    }

    /// <summary>Same as <see cref="UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>, configuring the checks to run via <paramref name="action"/>.</summary>
    public static IMiddlewarePipelineBuilder<TContext> UseLivenessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Action<IHealthCheckBuilder> action)
            where TContext : IHttpContext
    {
        return app.UseLivenessCheck("/livez", action);
    }

    /// <summary>Same as <see cref="UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, Action{IHealthCheckBuilder})"/>, at a custom <paramref name="path"/> instead of the conventional <c>/livez</c>.</summary>
    public static IMiddlewarePipelineBuilder<TContext> UseLivenessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string path, Action<IHealthCheckBuilder> action)
            where TContext : IHttpContext
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);
        return app.UseHealthCheck(Constants.DefaultLivenessTopic, "GET", path, builder);
    }

    /// <summary>
    /// Adds a Kubernetes-style readiness-check endpoint at <c>GET /readyz</c>, using
    /// <see cref="Constants.DefaultReadinessTopic"/>. See
    /// <see cref="Benzene.HealthChecks.Extensions.UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>
    /// for the liveness/readiness distinction.
    /// </summary>
    public static IMiddlewarePipelineBuilder<TContext> UseReadinessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, params IHealthCheck[] healthChecks)
            where TContext : IHttpContext
    {
        return app.UseReadinessCheck("/readyz", healthChecks);
    }

    /// <summary>Same as <see cref="UseReadinessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>, at a custom <paramref name="path"/> instead of the conventional <c>/readyz</c>.</summary>
    public static IMiddlewarePipelineBuilder<TContext> UseReadinessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string path, params IHealthCheck[] healthChecks)
            where TContext : IHttpContext
    {
        return app.UseReadinessCheck(path, x => x.AddHealthChecks(healthChecks));
    }

    /// <summary>Same as <see cref="UseReadinessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>, configuring the checks to run via <paramref name="action"/>.</summary>
    public static IMiddlewarePipelineBuilder<TContext> UseReadinessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Action<IHealthCheckBuilder> action)
            where TContext : IHttpContext
    {
        return app.UseReadinessCheck("/readyz", action);
    }

    /// <summary>Same as <see cref="UseReadinessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, Action{IHealthCheckBuilder})"/>, at a custom <paramref name="path"/> instead of the conventional <c>/readyz</c>.</summary>
    public static IMiddlewarePipelineBuilder<TContext> UseReadinessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string path, Action<IHealthCheckBuilder> action)
            where TContext : IHttpContext
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);
        return app.UseHealthCheck(Constants.DefaultReadinessTopic, "GET", path, builder);
    }
}
