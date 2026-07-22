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
        // The general healthcheck topic is the deep layer: it is the ONLY probe that harvests the
        // auto-wired dependency checks (monitoring / mesh / humans scrape it; it triggers no k8s action).
        return app.UseHealthCheckMiddleware(new[] { topic, Constants.DefaultHealthCheckTopic }, builder, includeDependencyChecks: true);
    }

    /// <summary>
    /// Adds Kubernetes-style liveness-check middleware to the pipeline: runs the given
    /// <paramref name="healthChecks"/> whenever an incoming message's topic matches
    /// <see cref="Constants.DefaultLivenessTopic"/>. Unlike <see cref="UseHealthCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, string, IHealthCheck[])"/>,
    /// this does NOT also respond to <see cref="Constants.DefaultHealthCheckTopic"/> - see
    /// <see cref="Constants.DefaultLivenessTopic"/> for why, and see
    /// <see cref="UseReadinessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>
    /// for the check to run alongside this one.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="healthChecks">
    /// The checks to run. A liveness check should verify only that this process itself is
    /// responsive - avoid checking external dependencies here (a database, a downstream service):
    /// per Kubernetes' own guidance, a liveness probe failure causes the pod to be restarted, and a
    /// flaky dependency shouldn't trigger restarts. Check external dependencies via
    /// <see cref="UseReadinessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>
    /// instead, whose failure only removes the pod from service (no restart).
    /// </param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseLivenessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, params IHealthCheck[] healthChecks)
    {
        return app.UseLivenessCheck(builder => builder.AddHealthChecks(healthChecks));
    }

    /// <summary>See <see cref="UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>; configures the checks to run via <paramref name="action"/> against a new <see cref="IHealthCheckBuilder"/>.</summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="action">Configures the health checks to register.</param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseLivenessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Action<IHealthCheckBuilder> action)
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);

        return app.UseLivenessCheck(builder);
    }

    /// <summary>See <see cref="UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>; uses an already-configured <paramref name="builder"/>.</summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="builder">Supplies the health checks to run, resolved per-request against the current <c>IServiceResolver</c>.</param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseLivenessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, IHealthCheckBuilder builder)
    {
        // Liveness deliberately excludes the auto-wired dependency checks: a downstream blip must not fail
        // liveness and restart the pod (§3.2).
        return app.UseHealthCheckMiddleware(new[] { Constants.DefaultLivenessTopic }, builder, includeDependencyChecks: false);
    }

    /// <summary>
    /// Adds Kubernetes-style readiness-check middleware to the pipeline: runs the given
    /// <paramref name="healthChecks"/> whenever an incoming message's topic matches
    /// <see cref="Constants.DefaultReadinessTopic"/>. See
    /// <see cref="UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>
    /// for the liveness/readiness distinction and why this doesn't also respond to
    /// <see cref="Constants.DefaultHealthCheckTopic"/>.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="healthChecks">
    /// The checks to run. This is the right place for checks against external dependencies (a
    /// database, a downstream HTTP service, a queue) - a readiness probe failure only removes the
    /// pod from service without restarting it, which is the appropriate response to a flaky
    /// dependency rather than restarting the process.
    /// </param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseReadinessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, params IHealthCheck[] healthChecks)
    {
        return app.UseReadinessCheck(builder => builder.AddHealthChecks(healthChecks));
    }

    /// <summary>See <see cref="UseReadinessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>; configures the checks to run via <paramref name="action"/> against a new <see cref="IHealthCheckBuilder"/>.</summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="action">Configures the health checks to register.</param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseReadinessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Action<IHealthCheckBuilder> action)
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);

        return app.UseReadinessCheck(builder);
    }

    /// <summary>See <see cref="UseReadinessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>; uses an already-configured <paramref name="builder"/>.</summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="builder">Supplies the health checks to run, resolved per-request against the current <c>IServiceResolver</c>.</param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseReadinessCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, IHealthCheckBuilder builder)
    {
        // Readiness ALSO excludes the auto-wired dependency checks. A dependency check is shared-fate -
        // every replica runs the same check against the same downstream - so gating readiness on it would
        // pull every pod from the Service at once on a transient blip (zero endpoints, connection-refused
        // to callers), turning a degraded dependency into a total outage. Readiness stays instance-local
        // ("can THIS pod serve"); dependency reachability is surfaced on the deep healthcheck layer. A
        // developer can still add a dependency check to readiness explicitly if they've reasoned it safe.
        return app.UseHealthCheckMiddleware(new[] { Constants.DefaultReadinessTopic }, builder, includeDependencyChecks: false);
    }

    /// <summary>
    /// Adds contract-check middleware to the pipeline: runs the given <paramref name="healthChecks"/>
    /// whenever an incoming message's topic matches <see cref="Constants.DefaultContractsTopic"/>.
    /// This is a <em>diagnostic</em> surface for consumer-side contract-drift / downstream-provider
    /// checks (see <c>Benzene.Clients.HealthChecks.AddContractCheck</c>), deliberately separate from
    /// the liveness/readiness probes: a contract check calls a downstream service and reports drift,
    /// so putting it in a probe would let one struggling dependency restart or de-route
    /// otherwise-healthy pods. Wire this to monitoring/the mesh, not to a Kubernetes probe. Like
    /// <see cref="UseLivenessCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>,
    /// it does NOT also respond to <see cref="Constants.DefaultHealthCheckTopic"/>.
    /// </summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="healthChecks">The checks to run - typically consumer-side contract-drift checks against downstream services.</param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseContractsCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, params IHealthCheck[] healthChecks)
    {
        return app.UseContractsCheck(builder => builder.AddHealthChecks(healthChecks));
    }

    /// <summary>See <see cref="UseContractsCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>; configures the checks to run via <paramref name="action"/> against a new <see cref="IHealthCheckBuilder"/>.</summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="action">Configures the health checks to register.</param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseContractsCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, Action<IHealthCheckBuilder> action)
    {
        var builder = app.GetHealthCheckerBuilder();
        action(builder);

        return app.UseContractsCheck(builder);
    }

    /// <summary>See <see cref="UseContractsCheck{TContext}(IMiddlewarePipelineBuilder{TContext}, IHealthCheck[])"/>; uses an already-configured <paramref name="builder"/>.</summary>
    /// <typeparam name="TContext">The pipeline's message-handling context type.</typeparam>
    /// <param name="app">The pipeline builder to add the middleware to.</param>
    /// <param name="builder">Supplies the health checks to run, resolved per-request against the current <c>IServiceResolver</c>.</param>
    /// <returns>The same pipeline builder, for chaining.</returns>
    public static IMiddlewarePipelineBuilder<TContext> UseContractsCheck<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, IHealthCheckBuilder builder)
    {
        // Contracts is a diagnostic topic for explicitly-added contract-drift checks only - the auto-wired
        // reachability checks (dependency category) must not pollute it.
        return app.UseHealthCheckMiddleware(new[] { Constants.DefaultContractsTopic }, builder, includeDependencyChecks: false);
    }

    /// <summary>
    /// Shared middleware implementation behind every <c>UseHealthCheck</c>/<c>UseLivenessCheck</c>/
    /// <c>UseReadinessCheck</c> overload: runs <paramref name="builder"/>'s checks and sets the
    /// aggregated result whenever the incoming message's topic is in <paramref name="matchTopics"/>,
    /// otherwise passes through to <c>next()</c>.
    /// </summary>
    private static IMiddlewarePipelineBuilder<TContext> UseHealthCheckMiddleware<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, string[] matchTopics, IHealthCheckBuilder builder, bool includeDependencyChecks)
    {
        return app.Use(resolver => new FuncWrapperMiddleware<TContext>(Constants.HealthCheckMiddlewareName, async (context, next) =>
        {
            var mapper = resolver.GetService<IMessageGetter<TContext>>();
            var messageTopic = mapper.GetTopic(context);

            if (matchTopics.Contains(messageTopic.Id))
            {
                // Resolve the result setter + processor only on the (rare) health-check topic - every
                // other message just passes through, so resolving these on that path was dead weight.
                var resultSetter = resolver.GetService<IMessageHandlerResultSetter<TContext>>();
                var processor = resolver.GetService<IHealthCheckProcessor>();
                var result = await processor.PerformHealthChecksAsync(builder.GetHealthChecks(resolver, includeDependencyChecks));
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
