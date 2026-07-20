using Confluent.Kafka;

namespace Benzene.Kafka.Core;

/// <summary>
/// Creates the underlying <see cref="IConsumer{TKey,TValue}"/> used by
/// <see cref="BenzeneKafkaWorker{TKey,TValue}"/>. The default implementation
/// (<see cref="KafkaConsumerFactory{TKey,TValue}"/>) just builds from the config; supply your own
/// to configure the <see cref="ConsumerBuilder{TKey,TValue}"/> in ways plain
/// <see cref="ConsumerConfig"/> can't express - deserializers, handlers, and notably
/// <c>SetOAuthBearerTokenRefreshHandler</c> for secretless OAUTHBEARER auth (e.g. Azure Entra ID
/// managed identity against Event Hubs' Kafka endpoint - see
/// <c>docs/cookbooks/managed-identity.md</c>). This is the Kafka counterpart of the Azure workers'
/// client-factory seams (<c>IEventProcessorClientFactory</c>, <c>IServiceBusClientFactory</c>, ...).
/// </summary>
/// <typeparam name="TKey">The consumer's key type.</typeparam>
/// <typeparam name="TValue">The consumer's value type.</typeparam>
public interface IKafkaConsumerFactory<TKey, TValue>
{
    /// <summary>
    /// Creates the consumer. The worker calls this once, on its background consume task, passing
    /// <c>BenzeneKafkaConfig.ConsumerConfig</c> <em>after</em> applying its own adjustments (e.g.
    /// <c>EnableAutoOffsetStore = false</c> for <c>CommitOnlyOnSuccess</c>) - build from the
    /// passed config, not a captured copy, so those adjustments are honored.
    /// </summary>
    /// <param name="consumerConfig">The consumer configuration to build from.</param>
    /// <returns>The created (not yet subscribed) consumer; the worker subscribes, closes, and disposes it.</returns>
    IConsumer<TKey, TValue> Create(ConsumerConfig consumerConfig);

    /// <summary>
    /// Creates the consumer, applying an extra worker-supplied <see cref="ConsumerBuilder{TKey,TValue}"/>
    /// configuration step (e.g. the rebalance handler that drives <c>DrainOnRevoke</c>) in addition to
    /// any the factory applies itself. The default implementation ignores <paramref name="configureBuilder"/>
    /// and falls back to <see cref="Create(ConsumerConfig)"/> - a custom factory built before this
    /// overload existed keeps working unchanged, but the worker's rebalance-drain handler is only wired
    /// when the factory honors this callback (the default <see cref="KafkaConsumerFactory{TKey,TValue}"/>
    /// does).
    /// </summary>
    /// <param name="consumerConfig">The consumer configuration to build from.</param>
    /// <param name="configureBuilder">A worker-supplied builder configuration step, or <c>null</c>.</param>
    /// <returns>The created (not yet subscribed) consumer.</returns>
    IConsumer<TKey, TValue> Create(ConsumerConfig consumerConfig, Action<ConsumerBuilder<TKey, TValue>>? configureBuilder)
        => Create(consumerConfig);
}

/// <summary>
/// Default <see cref="IKafkaConsumerFactory{TKey,TValue}"/>: builds a
/// <see cref="ConsumerBuilder{TKey,TValue}"/> from the passed config, applies the optional
/// configuration action, and builds - preserving the worker's original behavior exactly when no
/// action is supplied.
/// </summary>
/// <typeparam name="TKey">The consumer's key type.</typeparam>
/// <typeparam name="TValue">The consumer's value type.</typeparam>
public class KafkaConsumerFactory<TKey, TValue> : IKafkaConsumerFactory<TKey, TValue>
{
    private readonly Action<ConsumerBuilder<TKey, TValue>>? _configure;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaConsumerFactory{TKey,TValue}"/> class.
    /// </summary>
    /// <param name="configure">
    /// Optional builder configuration, applied before <c>Build()</c> - set deserializers, error
    /// handlers, or an OAuth bearer token refresh handler here.
    /// </param>
    public KafkaConsumerFactory(Action<ConsumerBuilder<TKey, TValue>>? configure = null)
    {
        _configure = configure;
    }

    /// <inheritdoc />
    public IConsumer<TKey, TValue> Create(ConsumerConfig consumerConfig) => Create(consumerConfig, null);

    /// <inheritdoc />
    public IConsumer<TKey, TValue> Create(ConsumerConfig consumerConfig, Action<ConsumerBuilder<TKey, TValue>>? configureBuilder)
    {
        var builder = new ConsumerBuilder<TKey, TValue>(consumerConfig);
        _configure?.Invoke(builder);
        configureBuilder?.Invoke(builder);
        return builder.Build();
    }
}
