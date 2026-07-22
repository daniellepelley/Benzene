using Confluent.Kafka;

namespace Benzene.Kafka.Core;

/// <summary>
/// Supplies the <see cref="IAdminClient"/> the <see cref="KafkaHealthCheck"/> uses for its metadata
/// read. The health check resolves ONE admin client through this seam and reuses it across probes —
/// building an admin client per probe would re-establish broker connections at scrape cadence. This is
/// the Kafka admin-side counterpart of <see cref="IKafkaConsumerFactory{TKey,TValue}"/>; supply your own
/// to configure the <see cref="AdminClientBuilder"/> in ways plain config can't express (notably
/// <c>SetOAuthBearerTokenRefreshHandler</c> for secretless OAUTHBEARER auth against Event Hubs' Kafka
/// endpoint — the same escape hatch the consumer factory offers).
/// </summary>
public interface IKafkaAdminClientFactory : IDisposable
{
    /// <summary>The reused admin client (built lazily on first access).</summary>
    IAdminClient AdminClient { get; }
}

/// <summary>
/// Default <see cref="IKafkaAdminClientFactory"/>: lazily builds one <see cref="IAdminClient"/> from the
/// connection/auth settings of the consumer's <see cref="ConsumerConfig"/> and reuses it. Only the
/// connection-relevant settings are copied (bootstrap servers + the common SASL/SSL fields) — the
/// consumer-only keys (<c>group.id</c>, <c>auto.offset.reset</c>, …) would make librdkafka reject the
/// admin config. For auth an <see cref="AdminClientConfig"/> can't express (OAUTHBEARER handlers, custom
/// certificate validation), pass a <paramref name="configure"/> callback or supply a custom factory.
/// </summary>
public class KafkaAdminClientFactory : IKafkaAdminClientFactory
{
    private readonly Lazy<IAdminClient> _adminClient;

    /// <summary>Initializes a new instance of the <see cref="KafkaAdminClientFactory"/> class.</summary>
    /// <param name="consumerConfig">The consumer config to copy connection/auth settings from.</param>
    /// <param name="configure">An optional builder step (e.g. an OAUTHBEARER token refresh handler).</param>
    public KafkaAdminClientFactory(ConsumerConfig consumerConfig, Action<AdminClientBuilder>? configure = null)
    {
        _adminClient = new Lazy<IAdminClient>(() =>
        {
            var builder = new AdminClientBuilder(ToAdminConfig(consumerConfig));
            configure?.Invoke(builder);
            return builder.Build();
        });
    }

    /// <inheritdoc />
    public IAdminClient AdminClient => _adminClient.Value;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_adminClient.IsValueCreated)
        {
            _adminClient.Value.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    // Copy only the connection/auth settings; consumer-only keys would be rejected by the admin client.
    private static AdminClientConfig ToAdminConfig(ConsumerConfig c) => new()
    {
        BootstrapServers = c.BootstrapServers,
        SecurityProtocol = c.SecurityProtocol,
        SaslMechanism = c.SaslMechanism,
        SaslUsername = c.SaslUsername,
        SaslPassword = c.SaslPassword,
        SaslOauthbearerConfig = c.SaslOauthbearerConfig,
        SslCaLocation = c.SslCaLocation,
        SslCertificateLocation = c.SslCertificateLocation,
        SslKeyLocation = c.SslKeyLocation,
        SslKeyPassword = c.SslKeyPassword,
        ClientId = c.ClientId,
    };
}
