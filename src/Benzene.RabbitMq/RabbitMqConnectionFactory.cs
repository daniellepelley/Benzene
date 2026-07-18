using RabbitMQ.Client;

namespace Benzene.RabbitMq;

/// <summary>
/// The default <see cref="IRabbitMqConnectionFactory"/>: opens a connection from a supplied
/// <see cref="ConnectionFactory"/>. Automatic recovery is on by <see cref="ConnectionFactory"/>'s own
/// default, so a dropped connection/channel is transparently re-established.
/// </summary>
public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    private readonly ConnectionFactory _connectionFactory;

    /// <summary>Initializes a new instance from an existing <see cref="ConnectionFactory"/>.</summary>
    /// <param name="connectionFactory">The configured RabbitMQ connection factory (host, credentials, vhost, TLS, ...).</param>
    public RabbitMqConnectionFactory(ConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>Initializes a new instance from an AMQP URI (e.g. <c>amqp://user:pass@host:5672/</c>).</summary>
    /// <param name="uri">The AMQP URI to connect to.</param>
    public RabbitMqConnectionFactory(Uri uri)
        : this(new ConnectionFactory { Uri = uri })
    {
    }

    /// <inheritdoc />
    public Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        return _connectionFactory.CreateConnectionAsync(cancellationToken);
    }
}
