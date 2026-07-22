using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Azure.QueueStorage;

/// <summary>
/// Verifies an Azure Storage queue is reachable with a read-only <c>GetProperties</c> call — the Queue
/// Storage analogue of <c>SqsHealthCheck</c>, non-side-effecting (it reads queue metadata; it does not
/// send, receive, peek, or delete a message). Proves the queue exists, is reachable, and the credential
/// can read it — not that a send would succeed (a send-only SAS may lack read). A permission error is
/// reported as a <see cref="HealthCheckStatus.Warning"/> (§3.9), and the SDK's error code + HTTP status
/// are surfaced in <c>Data</c> (never the exception message, which can carry a SAS token).
/// </summary>
public class QueueStorageHealthCheck : IHealthCheck
{
    private readonly QueueClient _queueClient;

    /// <summary>Initializes a new instance of the <see cref="QueueStorageHealthCheck"/> class.</summary>
    /// <param name="queueClient">The queue client to check (the caller owns its authentication and lifetime).</param>
    public QueueStorageHealthCheck(QueueClient queueClient)
    {
        _queueClient = queueClient;
    }

    /// <summary>The check's identifier: <c>"QueueStorage"</c>.</summary>
    public string Type => "QueueStorage";

    /// <summary>Runs the check and reports the outcome.</summary>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("Queue", _queueClient.Name) };

        try
        {
            // GetProperties is a read-only metadata read; the Azure SDK throws on any non-success, so a
            // returned response is itself the connectivity signal.
            await _queueClient.GetPropertiesAsync();
            return HealthCheckResult.CreateInstance(true, Type,
                new Dictionary<string, object> { { "Queue", _queueClient.Name } }, dependencies);
        }
        catch (Exception ex)
        {
            // Expected failures (queue missing, no connectivity, no permission) are a classified result,
            // not a throw. HealthCheckError applies the shared policy: 401/403 -> Warning, else Failed,
            // enriched with the SDK's error code + status, never the exception message.
            var (errorCode, statusCode) = AzureErrorDetails(ex);
            return HealthCheckError.Classify(Type, ex, dependencies, errorCode, statusCode,
                new Dictionary<string, object> { { "Queue", _queueClient.Name } });
        }
    }

    // Pulls the non-sensitive discriminators Azure already returns off a RequestFailedException; null for
    // a non-Azure exception (e.g. a raw socket failure).
    private static (string ErrorCode, int? StatusCode) AzureErrorDetails(Exception ex)
        => ex is RequestFailedException rfe ? (rfe.ErrorCode, (int?)rfe.Status) : (null, null);
}
