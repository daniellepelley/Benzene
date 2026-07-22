using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Azure.EventHub;

/// <summary>
/// Verifies an Azure Event Hub is reachable with a read-only <c>GetEventHubProperties</c> call — the
/// Event Hubs analogue of <c>SqsHealthCheck</c>, non-side-effecting (it reads hub metadata / partition
/// ids; it does not send an event). A permission failure is reported as a
/// <see cref="HealthCheckStatus.Warning"/> (§3.9). Event Hubs is AMQP, not HTTP, so there is no HTTP
/// status: the SDK's <see cref="EventHubsException.FailureReason"/> is surfaced as the error code, and an
/// <see cref="UnauthorizedAccessException"/> (how the SDK signals a bad credential/claim) is mapped to
/// <c>403</c> so it classifies as a permission Warning like the HTTP-based checks. The exception message
/// is never included.
/// </summary>
public class EventHubHealthCheck : IHealthCheck
{
    private readonly EventHubProducerClient _producerClient;
    private readonly string _eventHubName;

    /// <summary>Initializes a new instance of the <see cref="EventHubHealthCheck"/> class.</summary>
    /// <param name="producerClient">The producer client to check (the caller owns its authentication and lifetime).</param>
    /// <param name="eventHubName">
    /// The hub name reported as the dependency. Pass <c>producerClient.EventHubName</c>; it is taken as a
    /// parameter (rather than read off the client) because <see cref="EventHubProducerClient.EventHubName"/>
    /// is non-virtual and so cannot be substituted in a unit test.
    /// </param>
    public EventHubHealthCheck(EventHubProducerClient producerClient, string eventHubName)
    {
        _producerClient = producerClient;
        _eventHubName = eventHubName;
    }

    /// <summary>The check's identifier: <c>"EventHub"</c>.</summary>
    public string Type => "EventHub";

    /// <summary>Runs the check and reports the outcome.</summary>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("EventHub", _eventHubName) };

        try
        {
            // GetEventHubProperties is a read-only metadata read; the SDK throws on any failure, so a
            // returned response is itself the connectivity signal.
            await _producerClient.GetEventHubPropertiesAsync();
            return HealthCheckResult.CreateInstance(true, Type,
                new Dictionary<string, object> { { "EventHub", _eventHubName } }, dependencies);
        }
        catch (Exception ex)
        {
            // Expected failures (hub missing, no connectivity, no permission) are a classified result,
            // not a throw. HealthCheckError applies the shared policy: 401/403 -> Warning, else Failed,
            // enriched with the failure reason, never the exception message.
            var (errorCode, statusCode) = EventHubErrorDetails(ex);
            return HealthCheckError.Classify(Type, ex, dependencies, errorCode, statusCode,
                new Dictionary<string, object> { { "EventHub", _eventHubName } });
        }
    }

    // Event Hubs surfaces failures as EventHubsException with a FailureReason (AMQP, no HTTP status), and
    // a bad credential/claim as UnauthorizedAccessException. Map the latter to 403 so the shared policy
    // degrades a permission failure to Warning; report the EventHubsException reason as the error code
    // (no status -> Failed); null for anything else (e.g. a raw socket failure).
    private static (string ErrorCode, int? StatusCode) EventHubErrorDetails(Exception ex)
    {
        if (ex is UnauthorizedAccessException)
        {
            return ("Unauthorized", 403);
        }

        if (ex is EventHubsException ehEx)
        {
            return (ehEx.Reason.ToString(), null);
        }

        return (null, null);
    }
}
