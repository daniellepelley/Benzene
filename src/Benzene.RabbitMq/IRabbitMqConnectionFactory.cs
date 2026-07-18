using RabbitMQ.Client;

namespace Benzene.RabbitMq;

/// <summary>
/// The seam through which <see cref="RabbitMqWorker"/> (and the outbound publish client) obtains a
/// RabbitMQ <see cref="IConnection"/> - mirroring the Kafka/Service Bus client-factory seams
/// (<c>IKafkaConsumerFactory</c>, <c>IServiceBusClientFactory</c>). The caller owns how the
/// connection is built (host, credentials, TLS, virtual host, automatic recovery); the worker owns
/// its channel and disposes both the channel and the connection on stop.
/// </summary>
public interface IRabbitMqConnectionFactory
{
    /// <summary>Opens a new RabbitMQ connection.</summary>
    /// <param name="cancellationToken">A token to abort the connect.</param>
    /// <returns>The opened connection.</returns>
    Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken);
}
