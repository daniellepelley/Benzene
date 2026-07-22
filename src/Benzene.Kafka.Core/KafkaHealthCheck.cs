using Benzene.HealthChecks.Core;
using Confluent.Kafka;

namespace Benzene.Kafka.Core;

/// <summary>
/// Verifies a Kafka consumer can reach its cluster and that its subscribed topics exist, with a
/// read-only metadata request — the Kafka analogue of the AWS/Azure reachability checks, non-destructive
/// (it neither consumes, commits, nor produces). Proves the bootstrap brokers are reachable, the
/// credentials authenticate, and each configured topic is present. Reported on the <b>dependency</b>
/// category (deep <c>healthcheck</c> layer only — a broker being unreachable is shared-fate; see
/// <see cref="IDependencyHealthCheck"/>). A Kafka authorization failure is reported as a <b>persistent</b>
/// <see cref="HealthCheckStatus.Failed"/> — it surfaces as unhealthy even for the auto-wired dependency
/// check rather than being softened to a Warning (§3.9, reversed), since a bad credential/ACL is a
/// deterministic misconfiguration that won't self-heal; the exception message is never included.
/// </summary>
public class KafkaHealthCheck : IHealthCheck
{
    /// <summary>The default librdkafka metadata-request timeout.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly IKafkaAdminClientFactory _adminClientFactory;
    private readonly string _bootstrapServers;
    private readonly string[] _topics;
    private readonly TimeSpan _timeout;

    /// <summary>Initializes a new instance of the <see cref="KafkaHealthCheck"/> class.</summary>
    /// <param name="adminClientFactory">Supplies the reused admin client used for the metadata read.</param>
    /// <param name="bootstrapServers">The cluster's bootstrap servers, reported as the broker dependency.</param>
    /// <param name="topics">The subscribed topics whose existence to verify.</param>
    /// <param name="metadataTimeout">The librdkafka metadata-request timeout. Defaults to <see cref="DefaultTimeout"/>.</param>
    public KafkaHealthCheck(IKafkaAdminClientFactory adminClientFactory, string bootstrapServers, string[] topics,
        TimeSpan? metadataTimeout = null)
    {
        _adminClientFactory = adminClientFactory;
        _bootstrapServers = bootstrapServers;
        _topics = topics ?? Array.Empty<string>();
        _timeout = metadataTimeout ?? DefaultTimeout;
    }

    /// <summary>The check's identifier: <c>"Kafka"</c>.</summary>
    public string Type => "Kafka";

    /// <summary>Runs the check and reports the outcome.</summary>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new List<HealthCheckDependency> { new("Broker", _bootstrapServers) };
        foreach (var topic in _topics)
        {
            dependencies.Add(new HealthCheckDependency("Topic", topic));
        }
        var deps = dependencies.ToArray();

        try
        {
            // GetMetadata is a synchronous librdkafka call bounded by its own timeout; offload it so the
            // processor's timeout wrapper can still race it. Cluster-level (no topic argument) so it never
            // triggers the broker-side auto-topic-creation a per-topic metadata request can.
            var metadata = await Task.Run(() => _adminClientFactory.AdminClient.GetMetadata(_timeout));

            var missing = _topics
                .Where(t => !metadata.Topics.Any(tm => tm.Topic == t && tm.Error.Code == ErrorCode.NoError))
                .ToArray();

            var data = new Dictionary<string, object>
            {
                { "Broker", _bootstrapServers },
                { "Brokers", metadata.Brokers.Count },
            };

            if (missing.Length > 0)
            {
                data["MissingTopics"] = string.Join(",", missing);
                return HealthCheckResult.CreateInstance(false, Type, data, deps);
            }

            return HealthCheckResult.CreateInstance(true, Type, data, deps);
        }
        catch (Exception ex)
        {
            // Expected failures (broker unreachable, timed out, not authorized) are a classified result,
            // not a throw. HealthCheckError applies the shared policy: an authorization failure is a
            // persistent Failed, anything else a transient Failed, enriched with the Kafka error code,
            // never the exception message.
            var (errorCode, statusCode) = KafkaErrorDetails(ex);
            return HealthCheckError.Classify(Type, ex, deps, errorCode, statusCode,
                new Dictionary<string, object> { { "Broker", _bootstrapServers } });
        }
    }

    // Kafka is not HTTP; failures surface as KafkaException with an ErrorCode. Report the code name, and
    // map the authorization codes to 403 so the shared policy classifies them as a persistent Failed;
    // null otherwise.
    private static (string? ErrorCode, int? StatusCode) KafkaErrorDetails(Exception ex)
    {
        if (ex is KafkaException kex)
        {
            var code = kex.Error.Code;
            var isAuth = code is ErrorCode.TopicAuthorizationFailed or ErrorCode.GroupAuthorizationFailed
                or ErrorCode.ClusterAuthorizationFailed or ErrorCode.SaslAuthenticationFailed;
            return (code.ToString(), isAuth ? 403 : (int?)null);
        }

        return (null, null);
    }
}
