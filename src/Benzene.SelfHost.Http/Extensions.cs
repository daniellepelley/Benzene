using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.HealthChecks;
using Benzene.HealthChecks.Core;
using Benzene.Http;
using Constants = Benzene.HealthChecks.Constants;

namespace Benzene.SelfHost.Http;

public static class Extensions
{
    public static IBenzeneWorkerBuilder UseHttp(this IBenzeneWorkerBuilder app, BenzeneHttpConfig benzeneHttpConfig, Action<IMiddlewarePipelineBuilder<SelfHostHttpContext>> action)
    {
        app.Register(x => x.AddHttp());
        var middlewarePipelineBuilder = app.Create<SelfHostHttpContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        var httpApplication = new HttpListenerApplication(pipeline);
        app.Add(serviceResolverFactory => new BenzeneHttpWorker(serviceResolverFactory, httpApplication, benzeneHttpConfig));
        return app;
    }
    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string method, string path,
        params IHealthCheck[] healthChecks)
            where TContext : IHttpContext, IHasMessageResult
    {
        return app.UseHealthCheck(Constants.DefaultHealthCheckTopic, method, path, healthChecks);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string topic, string method, string path,
        params IHealthCheck[] healthChecks)
            where TContext : IHttpContext, IHasMessageResult
    {
        return app.UseHealthCheck(topic, method, path, x => x.AddHealthChecks(healthChecks));
    }

    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(this IMiddlewarePipelineBuilder<TContext> app, string method, string path, Action<IHealthCheckBuilder> action)
        where TContext : IHttpContext, IHasMessageResult
    {
        return app.UseHealthCheck(Constants.DefaultHealthCheckTopic, method, path, action);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(this IMiddlewarePipelineBuilder<TContext> app, string topic, string method, string path, Action<IHealthCheckBuilder> action)
        where TContext : IHttpContext, IHasMessageResult
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);
        return app.UseHealthCheck(topic, method, path, builder);
    }

    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(this IMiddlewarePipelineBuilder<TContext> app, string topic, string method, string path, IHealthCheckBuilder builder)
        where TContext : IHttpContext, IHasMessageResult
    {
        return app.Use(resolver => new FuncWrapperMiddleware<TContext>("HealthCheck", async (context, next) =>
        {
            var httpRequestAdapter = resolver.GetService<IHttpRequestAdapter<TContext>>();
            var resultSetter = resolver.GetService<IResultSetter<TContext>>();
            var httpRequest = httpRequestAdapter.Map(context);
            if (httpRequest.Method.ToUpperInvariant() == method.ToUpperInvariant() &&
                httpRequest.Path == path)
            {
                var result = await HealthCheckProcessor.PerformHealthChecksAsync(topic, builder.GetHealthChecks(resolver));
                resultSetter.SetResult(context, result, new Topic(topic), MessageHandlerDefinition.Empty());
            }
            else
            {
                await next();
            }
        }));
    }

}
