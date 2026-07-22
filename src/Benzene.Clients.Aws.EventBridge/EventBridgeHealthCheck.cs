using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.Runtime;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.EventBridge;

/// <summary>
/// Verifies an EventBridge event bus is reachable with a read-only <c>DescribeEventBus</c> call — the
/// EventBridge analogue of <c>SqsHealthCheck</c>/<c>SnsHealthCheck</c>, non-side-effecting (it does not
/// <c>PutEvents</c>, which would fire real rules/targets). Proves the bus exists, is reachable, and the
/// credentials can read it (<c>events:DescribeEventBus</c>) — not that a publish would succeed
/// (<c>events:PutEvents</c> is a different permission). A permission error is reported as a
/// <see cref="HealthCheckStatus.Warning"/> (§3.9), and the SDK's error code + HTTP status are surfaced in
/// <c>Data</c> (never the exception message).
/// </summary>
public class EventBridgeHealthCheck : IHealthCheck
{
    /// <summary>The name AWS uses for the account's default event bus.</summary>
    public const string DefaultEventBusName = "default";

    private readonly IAmazonEventBridge _eventBridge;
    private readonly string _eventBusName;

    /// <summary>Initializes a new instance of the <see cref="EventBridgeHealthCheck"/> class.</summary>
    /// <param name="eventBridge">The EventBridge client used to run the check.</param>
    /// <param name="eventBusName">The event bus to check; null/empty targets the account's default bus.</param>
    public EventBridgeHealthCheck(IAmazonEventBridge eventBridge, string eventBusName = null)
    {
        _eventBridge = eventBridge;
        _eventBusName = string.IsNullOrEmpty(eventBusName) ? DefaultEventBusName : eventBusName;
    }

    /// <summary>The check's identifier: <c>"EventBridge"</c>.</summary>
    public string Type => "EventBridge";

    /// <summary>Runs the check and reports the outcome.</summary>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("EventBus", _eventBusName) };

        try
        {
            var response = await _eventBridge.DescribeEventBusAsync(new DescribeEventBusRequest { Name = _eventBusName });
            return HealthCheckResult.CreateInstance(response.HttpStatusCode == HttpStatusCode.OK, Type,
                new Dictionary<string, object> { { "EventBus", _eventBusName } }, dependencies);
        }
        catch (Exception ex)
        {
            // Expected failures (bus missing, no connectivity, no permission) are a classified result,
            // not a throw. HealthCheckError applies the shared policy: 401/403 -> Warning, else Failed,
            // enriched with the SDK's error code + status, never the exception message.
            var (errorCode, statusCode) = AwsErrorDetails(ex);
            return HealthCheckError.Classify(Type, ex, dependencies, errorCode, statusCode,
                new Dictionary<string, object> { { "EventBus", _eventBusName } });
        }
    }

    // Pulls the non-sensitive discriminators AWS already returns off an SDK exception; null for a
    // non-AWS exception (e.g. a raw connectivity failure).
    private static (string ErrorCode, int? StatusCode) AwsErrorDetails(Exception ex)
        => ex is AmazonServiceException ase ? (ase.ErrorCode, (int?)(int)ase.StatusCode) : (null, null);
}
