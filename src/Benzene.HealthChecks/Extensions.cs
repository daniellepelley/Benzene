using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MiddlewareBuilder;
using Benzene.Abstractions.Results;
using Benzene.Core.Middleware;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

public static class Extensions
{
    public static IMiddlewarePipelineBuilder<THasMessageResult> UseHealthCheck<THasMessageResult>(
        this IMiddlewarePipelineBuilder<THasMessageResult> app, string topic, params IHealthCheck[] healthChecks)
        where THasMessageResult : IHasMessageResult
    {
        return app.UseHealthCheck(topic, builder => builder.AddHealthChecks(healthChecks));
    }

    public static IMiddlewarePipelineBuilder<THasMessageResult> UseHealthCheck<THasMessageResult>(
        this IMiddlewarePipelineBuilder<THasMessageResult> app, string topic, Action<IHealthCheckBuilder> action)
        where THasMessageResult : IHasMessageResult
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);

        return app.UseHealthCheck(topic, builder);
    }

    public static IMiddlewarePipelineBuilder<THasMessageResult> UseHealthCheck<THasMessageResult>(
        this IMiddlewarePipelineBuilder<THasMessageResult> app, string topic, IHealthCheckBuilder builder)
        where THasMessageResult : IHasMessageResult
    {
        return app.Use(resolver => new FuncWrapperMiddleware<THasMessageResult>(Constants.HealthCheckMiddlewareName, async (context, next) =>
        {
            var mapper = resolver.GetService<IMessageMapper<THasMessageResult>>();

            var messageTopic = mapper.GetTopic(context);

            if (new [] { topic, Constants.DefaultHealthCheckTopic }.Contains(messageTopic.Id))
            {
                context.MessageResult =
                    await HealthCheckProcessor.PerformHealthChecksAsync(topic,
                        builder.GetHealthChecks(resolver));
            }
            else
            {
                await next();
            }
        }));
    }

    public static IHealthCheckBuilder GetHealthCheckerBuilder(
        this IRegisterDependency registerDependency)
    {
        return new HealthCheckBuilder(registerDependency);
    }

    public static IHealthCheck BuildHealthCheck(Func<IHealthCheck> func)
    {
        try
        {
            return func();
        }
        catch(Exception ex)
        {
            return new FailedHealthCheck(ex);
        }
    }
}
