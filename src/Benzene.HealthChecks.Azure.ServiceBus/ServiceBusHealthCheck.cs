using Azure.Messaging.ServiceBus;
using Benzene.HealthChecks.Core;

namespace Benzene.HealthChecks.Azure.ServiceBus;

/// <summary>
/// Verifies an Azure Service Bus entity (a queue, or a topic subscription) is reachable with a
/// read-only <c>PeekMessage</c> call - the Service Bus analogue of <c>SqsHealthCheck</c>, but
/// non-side-effecting: peek neither locks, completes, nor removes a message, and returns null on an
/// empty entity, so a successful round-trip is the connectivity signal without disturbing the queue.
/// Needs only the data-plane <c>Listen</c> claim the consumer already holds (no management-plane
/// permission). A short-lived <see cref="ServiceBusReceiver"/> link is opened per probe and disposed.
/// </summary>
public class ServiceBusHealthCheck : IHealthCheck
{
    private readonly ServiceBusClient _client;
    private readonly string? _queueName;
    private readonly string? _topicName;
    private readonly string? _subscriptionName;
    private readonly HealthCheckDependency _dependency;

    /// <summary>Initializes a check for a Service Bus <paramref name="queueName"/>.</summary>
    /// <param name="client">The Service Bus client (the caller owns its authentication and lifetime).</param>
    /// <param name="queueName">The queue to check.</param>
    public ServiceBusHealthCheck(ServiceBusClient client, string queueName)
    {
        _client = client;
        _queueName = queueName;
        _dependency = new HealthCheckDependency("Queue", queueName);
    }

    /// <summary>Initializes a check for a topic subscription (<paramref name="topicName"/>/<paramref name="subscriptionName"/>).</summary>
    /// <param name="client">The Service Bus client (the caller owns its authentication and lifetime).</param>
    /// <param name="topicName">The topic the subscription belongs to.</param>
    /// <param name="subscriptionName">The subscription to check.</param>
    public ServiceBusHealthCheck(ServiceBusClient client, string topicName, string subscriptionName)
    {
        _client = client;
        _topicName = topicName;
        _subscriptionName = subscriptionName;
        _dependency = new HealthCheckDependency("Subscription", $"{topicName}/{subscriptionName}");
    }

    /// <inheritdoc />
    public string Type => "ServiceBus";

    /// <inheritdoc />
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { _dependency };
        ServiceBusReceiver? receiver = null;

        try
        {
            receiver = _queueName != null
                ? _client.CreateReceiver(_queueName)
                : _client.CreateReceiver(_topicName!, _subscriptionName!);

            // Peek is read-only and non-destructive: it neither locks nor removes the message, and
            // returns null on an empty entity - a successful round-trip is the connectivity signal.
            await receiver.PeekMessageAsync();

            return HealthCheckResult.CreateInstance(true, Type, BuildData(), dependencies);
        }
        catch (Exception ex)
        {
            // Expected failures (entity missing, no connectivity, no Listen claim) are a classified
            // result, not a throw. HealthCheckError applies the shared policy: an authorization failure
            // is a persistent Failed, anything else a transient Failed, reporting the SDK failure reason,
            // never the exception message.
            var (errorCode, statusCode) = ServiceBusErrorDetails(ex);
            return HealthCheckError.Classify(Type, ex, dependencies, errorCode, statusCode, BuildData());
        }
        finally
        {
            if (receiver != null)
            {
                await receiver.DisposeAsync();
            }
        }
    }

    // Service Bus is AMQP, not HTTP. A bad credential/claim surfaces as UnauthorizedAccessException
    // (mapped to 403 so the shared policy classifies it as a persistent Failed); other failures surface
    // as ServiceBusException with a FailureReason (reported as the error code, no status -> transient
    // Failed); null for anything else (e.g. a raw socket failure).
    private static (string? ErrorCode, int? StatusCode) ServiceBusErrorDetails(Exception ex)
    {
        if (ex is UnauthorizedAccessException)
        {
            return ("Unauthorized", 403);
        }

        if (ex is ServiceBusException sbe)
        {
            return (sbe.Reason.ToString(), null);
        }

        return (null, null);
    }

    private Dictionary<string, object> BuildData()
    {
        var data = new Dictionary<string, object> { { "Entity", _dependency.Name } };

        // The fully-qualified namespace host (e.g. myns.servicebus.windows.net) is not a secret - it
        // identifies which bus the check targets, useful in a mesh view. Guarded because a mocked
        // client leaves it null.
        var ns = _client.FullyQualifiedNamespace;
        if (!string.IsNullOrEmpty(ns))
        {
            data["Namespace"] = ns;
        }

        return data;
    }
}
