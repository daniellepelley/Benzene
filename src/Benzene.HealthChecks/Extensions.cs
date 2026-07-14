using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Middleware;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks;

/// <summary>Extension methods for wiring health checks into a Benzene middleware pipeline.</summary>
public static class Extensions
{
    /// <summary>
    /// Adds health check middleware to the pipeline that runs the given <paramref name="healthChecks"/>
    /// whenever an incoming message's topic matches <paramref name="topic"/> or
    /// <see cref="Constants.DefaultHealthCheckTopic"/>.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="topic">The topic that triggers this health check middleware, in addition to <see cref="Constants.DefaultHealthCheckTopic"/>.</param>
    /// <param name="healthChecks">The health checks to run.</param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string topic, params IHealthCheck[] healthChecks)
    {
        return app.UseHealthCheck(topic, builder => builder.AddHealthChecks(healthChecks));
    }

    /// <summary>
    /// Adds health check middleware to the pipeline, configuring the set of checks to run via
    /// <paramref name="action"/> against a new <see cref="IHealthCheckBuilder"/>. The middleware responds
    /// to messages whose topic matches <paramref name="topic"/> or <see cref="Constants.DefaultHealthCheckTopic"/>.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="topic">The topic that triggers this health check middleware, in addition to <see cref="Constants.DefaultHealthCheckTopic"/>.</param>
    /// <param name="action">Configures the health checks to register.</param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string topic, Action<IHealthCheckBuilder> action)
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);

        return app.UseHealthCheck(topic, builder);
    }

    /// <summary>
    /// Adds health check middleware to the pipeline using an already-configured <paramref name="builder"/>.
    /// For each incoming message, if its topic matches <paramref name="topic"/> or
    /// <see cref="Constants.DefaultHealthCheckTopic"/>, every health check resolved from
    /// <paramref name="builder"/> is run via <see cref="HealthCheckProcessor.PerformHealthChecksAsync"/>
    /// and the aggregated result is set as the message result; otherwise the message is passed to the
    /// next middleware in the pipeline.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="topic">The topic that triggers this health check middleware, in addition to <see cref="Constants.DefaultHealthCheckTopic"/>.</param>
    /// <param name="builder">Supplies the health checks to run, resolved per-request against the current <c>IServiceResolver</c>.</param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseHealthCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string topic, IHealthCheckBuilder builder)
    {
        return app.Use(resolver => new FuncWrapperMiddleware<TContext>(Constants.HealthCheckMiddlewareName, async (context, next) =>
        {
            var mapper = resolver.GetService<IMessageGetter<TContext>>();
            var resultSetter = resolver.GetService<IMessageHandlerResultSetter<TContext>>();

            var messageTopic = mapper.GetTopic(context);

            if (new [] { topic, Constants.DefaultHealthCheckTopic }.Contains(messageTopic.Id))
            {
                var result =
                    await HealthCheckProcessor.PerformHealthChecksAsync(topic,
                        builder.GetHealthChecks(resolver));
                await resultSetter.SetResultAsync(context, new MessageHandlerResult( messageTopic, MessageHandlerDefinition.Empty(), result));
            }
            else
            {
                await next();
            }
        }));
    }

    /// <summary>Creates a new <see cref="IHealthCheckBuilder"/> backed by the given dependency registry.</summary>
    /// <param name="registerDependency">The registry used to register services (e.g. <see cref="IHealthCheckFinder"/>) needed to resolve health checks.</param>
    /// <returns>A new, empty <see cref="HealthCheckBuilder"/>.</returns>
    public static IHealthCheckBuilder GetHealthCheckerBuilder(
        this IRegisterDependency registerDependency)
    {
        return new HealthCheckBuilder(registerDependency);
    }

    /// <summary>
    /// Invokes <paramref name="func"/> to construct a health check, catching any exception it throws and
    /// returning a <see cref="FailedHealthCheck"/> in its place instead of propagating the exception. Use
    /// this when the construction of a health check (e.g. its constructor) can itself fail.
    /// </summary>
    /// <param name="func">Constructs the health check.</param>
    /// <returns>The constructed health check, or a <see cref="FailedHealthCheck"/> wrapping any exception thrown by <paramref name="func"/>.</returns>
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
