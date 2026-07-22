using Benzene.Abstractions.DI;
using Benzene.HealthChecks.Core;

namespace Benzene.Kafka.Core;

/// <summary>
/// Registration helpers for <see cref="KafkaHealthCheck"/>.
/// </summary>
public static class KafkaHealthCheckExtensions
{
    /// <summary>
    /// Registers a <see cref="KafkaHealthCheck"/> for <paramref name="config"/> on an explicit health-check
    /// builder (e.g. inside <c>.UseHealthCheck(b =&gt; b.AddKafkaHealthCheck(config))</c>). Captures a single
    /// reused <see cref="KafkaAdminClientFactory"/>.
    /// </summary>
    /// <param name="builder">The health check builder to register against.</param>
    /// <param name="config">The Kafka consumer config (broker + auth) and subscribed topics to verify.</param>
    /// <returns>The health check builder for method chaining.</returns>
    public static IHealthCheckBuilder AddKafkaHealthCheck(this IHealthCheckBuilder builder, BenzeneKafkaConfig config)
    {
        var factory = new KafkaAdminClientFactory(config.ConsumerConfig);
        return builder.AddHealthCheck(_ =>
            new KafkaHealthCheck(factory, config.ConsumerConfig.BootstrapServers, config.Topics));
    }

    /// <summary>
    /// Auto-registers a <see cref="KafkaHealthCheck"/> for <paramref name="config"/> on the <b>dependency</b>
    /// category (deep <c>healthcheck</c> layer only — never a Kubernetes probe; see
    /// <see cref="IDependencyHealthCheck"/>), deduped by the bootstrap servers. Called by
    /// <c>UseKafka(..., healthCheck: true)</c>. A single <see cref="KafkaAdminClientFactory"/> is captured
    /// and reused across probes (no admin client is built per probe).
    /// </summary>
    /// <param name="services">The service container to register on.</param>
    /// <param name="config">The Kafka consumer config and subscribed topics.</param>
    /// <returns>The service container, for chaining.</returns>
    public static IBenzeneServiceContainer AddKafkaDependencyHealthCheck(this IBenzeneServiceContainer services, BenzeneKafkaConfig config)
    {
        var factory = new KafkaAdminClientFactory(config.ConsumerConfig);
        return services.AddDependencyHealthCheck(
            _ => new KafkaHealthCheck(factory, config.ConsumerConfig.BootstrapServers, config.Topics),
            $"Kafka:{config.ConsumerConfig.BootstrapServers}");
    }
}
