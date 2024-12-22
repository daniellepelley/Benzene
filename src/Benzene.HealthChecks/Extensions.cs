using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
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
    {
        return app.Use(resolver => new FuncWrapperMiddleware<THasMessageResult>(Constants.HealthCheckMiddlewareName, async (context, next) =>
        {
            var mapper = resolver.GetService<IMessageMapper<THasMessageResult>>();
            var resultSetter = resolver.GetService<IResultSetter<THasMessageResult>>();

            var messageTopic = mapper.GetTopic(context);

            if (new [] { topic, Constants.DefaultHealthCheckTopic }.Contains(messageTopic.Id))
            {
                var result =
                    await HealthCheckProcessor.PerformHealthChecksAsync(topic,
                        builder.GetHealthChecks(resolver));
                resultSetter.SetResultAsync(context, new MessageHandlerResult( messageTopic, MessageHandlerDefinition.Empty(), result));
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
