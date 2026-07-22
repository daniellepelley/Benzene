using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.RabbitMq;

/// <summary>
/// Registration helpers for <see cref="RabbitMqHealthCheck"/>.
/// </summary>
public static class RabbitMqHealthCheckExtensions
{
    /// <summary>
    /// Registers a <see cref="RabbitMqHealthCheck"/> for <paramref name="config"/>'s queue on an explicit
    /// health-check builder (e.g. <c>.UseHealthCheck(b =&gt; b.AddRabbitMqHealthCheck(config, factory))</c>).
    /// Captures a single reused <see cref="RabbitMqConnectionProvider"/>.
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="config">The RabbitMQ consumer config (its <c>QueueName</c> is verified).</param>
    /// <param name="connectionFactory">The factory used to open the (reused) health-check connection.</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddRabbitMqHealthCheck(this IHealthCheckBuilder builder, RabbitMqConfig config,
        IRabbitMqConnectionFactory connectionFactory)
    {
        var provider = new RabbitMqConnectionProvider(connectionFactory);
        return builder.AddHealthCheck(_ => new RabbitMqHealthCheck(provider, config.QueueName));
    }

    /// <summary>
    /// Auto-registers a <see cref="RabbitMqHealthCheck"/> for <paramref name="config"/>'s queue on the
    /// <b>dependency</b> category (deep <c>healthcheck</c> layer only — never a Kubernetes probe; see
    /// <see cref="IDependencyHealthCheck"/>), deduped by the queue name. Called by
    /// <c>UseRabbitMq(..., healthCheck: true)</c>. A single <see cref="RabbitMqConnectionProvider"/> is
    /// captured and reused across probes (one connection, a cheap channel per probe).
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="config">The RabbitMQ consumer config.</param>
    /// <param name="connectionFactory">The factory used to open the (reused) health-check connection.</param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddRabbitMqDependencyHealthCheck(this IBenzeneServiceContainer services,
        RabbitMqConfig config, IRabbitMqConnectionFactory connectionFactory)
    {
        var provider = new RabbitMqConnectionProvider(connectionFactory);
        return services.AddDependencyHealthCheck(
            _ => new RabbitMqHealthCheck(provider, config.QueueName),
            $"RabbitMq:{config.QueueName}");
    }
}
