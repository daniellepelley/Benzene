using Benzene.Abstractions.Middleware;
using Benzene.Core;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.Middleware;
using Benzene.RabbitMq.RabbitMqMessage;
using Benzene.SelfHost;
using Microsoft.Extensions.Logging;

namespace Benzene.RabbitMq;

/// <summary>
/// Adds a self-hosted RabbitMQ consumer to a Benzene worker, mirroring <c>UseKafka</c>/<c>UseServiceBus</c>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds a RabbitMQ consumer to the worker.
    /// </summary>
    /// <param name="app">The worker startup to add the RabbitMQ consumer to.</param>
    /// <param name="config">The queue to consume and the processing behavior to use.</param>
    /// <param name="connectionFactory">The factory used to open the RabbitMQ connection.</param>
    /// <param name="action">Configures the inner RabbitMQ message pipeline.</param>
    /// <param name="healthCheck">
    /// When <c>true</c> (the default) a non-destructive RabbitMQ reachability check (passive declare of
    /// the consumed queue) is auto-registered on the deep <c>healthcheck</c> layer — never a Kubernetes
    /// probe (a broker being unreachable is shared-fate; see <c>IDependencyHealthCheck</c>). Pass
    /// <c>false</c> to opt out.
    /// </param>
    /// <returns>The worker startup for method chaining.</returns>
    public static IBenzeneWorkerStartup UseRabbitMq(this IBenzeneWorkerStartup app, RabbitMqConfig config,
        IRabbitMqConnectionFactory connectionFactory, Action<IMiddlewarePipelineBuilder<RabbitMqContext>> action,
        bool healthCheck = true)
    {
        app.Register(x => x
            .AddBenzeneMessage()
            .AddRabbitMq(config.TopicHeaderKey)
        );

        if (healthCheck)
        {
            app.Register(x => x.AddRabbitMqDependencyHealthCheck(config, connectionFactory));
        }

        var middlewarePipelineBuilder = app.Create<RabbitMqContext>();
        // Seed the scope's ambient cancellation token from the delivery's token, so any component
        // resolving ICancellationTokenAccessor observes cancellation.
        middlewarePipelineBuilder.Use(resolver => new FuncWrapperMiddleware<RabbitMqContext>("SeedCancellationToken", async (context, next) =>
        {
            resolver.SeedCancellationToken(context.DeliverEventArgs.CancellationToken);
            await next();
        }));
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        var application = new RabbitMqApplication(pipeline);
        // Register the built application so it can be resolved and driven directly - e.g. a
        // StartUp-based component test pushing a delivery through the real pipeline without a running
        // broker (see Benzene.RabbitMq.TestHelpers). Inert in a normal worker run; the worker already
        // holds this same instance via the factory below.
        app.Register(x => x.AddSingleton(application));
        app.Add(serviceResolverFactory =>
        {
            using var scope = serviceResolverFactory.CreateScope();
            var logger = scope.GetService<ILogger<RabbitMqWorker>>();
            return new RabbitMqWorker(serviceResolverFactory, application, config, connectionFactory, logger);
        });

        return app;
    }
}
