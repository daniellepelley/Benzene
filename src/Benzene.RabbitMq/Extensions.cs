using Benzene.Abstractions.Middleware;
using Benzene.Core.MessageHandlers.DI;
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
    /// <returns>The worker startup for method chaining.</returns>
    public static IBenzeneWorkerStartup UseRabbitMq(this IBenzeneWorkerStartup app, RabbitMqConfig config,
        IRabbitMqConnectionFactory connectionFactory, Action<IMiddlewarePipelineBuilder<RabbitMqContext>> action)
    {
        app.Register(x => x
            .AddBenzeneMessage()
            .AddRabbitMq()
        );

        var middlewarePipelineBuilder = app.Create<RabbitMqContext>();
        action(middlewarePipelineBuilder);
        var pipeline = middlewarePipelineBuilder.Build();

        var application = new RabbitMqApplication(pipeline);
        app.Add(serviceResolverFactory =>
        {
            using var scope = serviceResolverFactory.CreateScope();
            var logger = scope.GetService<ILogger<RabbitMqWorker>>();
            return new RabbitMqWorker(serviceResolverFactory, application, config, connectionFactory, logger);
        });

        return app;
    }
}
