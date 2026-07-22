using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.Sqs;

/// <summary>
/// Verifies connectivity to an SQS queue. In the default <see cref="HealthCheckMode.Reachability"/> mode
/// this is a <b>non-destructive</b> read-only <c>GetQueueAttributes</c> call; in
/// <see cref="HealthCheckMode.Active"/> mode it sends a real <c>ping</c> message (side-effecting — the
/// queue's consumer must recognise and drop it).
/// </summary>
/// <remarks>
/// The reachability check proves the queue exists, is reachable, and the credentials can read it
/// (<c>sqs:GetQueueAttributes</c>) — it does <b>not</b> prove a send would succeed (<c>sqs:SendMessage</c>
/// is a different permission). Use <see cref="HealthCheckMode.Active"/> only when you need to exercise the
/// send path, and keep it off a frequent poll and off liveness/readiness probes.
/// </remarks>
public class SqsHealthCheck : IHealthCheck
{
    private readonly IAmazonSQS _amazonSqs;
    private readonly string _queueUrl;
    private readonly HealthCheckMode _mode;
    private readonly string _topicAttributeKey;
    private const int TimeOut = 10000;

    /// <summary>Initializes a new instance of the <see cref="SqsHealthCheck"/> class.</summary>
    /// <param name="queueUrl">The URL of the queue to check.</param>
    /// <param name="amazonSqs">The SQS client used to run the check.</param>
    /// <param name="mode">Reachability (default, read-only) or Active (sends a ping — side-effecting).</param>
    /// <param name="topicAttributeKey">
    /// Active mode only: the message attribute the ping topic is written to. Defaults to
    /// <see cref="OutboundSqsContextConverter.DefaultTopicAttribute"/> (<c>"topic"</c>) — pass the same
    /// key the queue's consumer routes on so the ping is routable there too.
    /// </param>
    public SqsHealthCheck(string queueUrl, IAmazonSQS amazonSqs,
        HealthCheckMode mode = HealthCheckMode.Reachability,
        string topicAttributeKey = OutboundSqsContextConverter.DefaultTopicAttribute)
    {
        _queueUrl = queueUrl;
        _amazonSqs = amazonSqs;
        _mode = mode;
        _topicAttributeKey = topicAttributeKey;
    }

    /// <summary>Runs the check and reports the outcome.</summary>
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("Queue", _queueUrl) };
        var call = _mode == HealthCheckMode.Active
            ? MapStatus(SendPingAsync())
            : MapStatus(_amazonSqs.GetQueueAttributesAsync(new GetQueueAttributesRequest
            {
                QueueUrl = _queueUrl,
                AttributeNames = new List<string> { "QueueArn" }
            }));

        return RunAsync(call, dependencies);
    }

    private Task<SendMessageResponse> SendPingAsync()
        => _amazonSqs.SendMessageAsync(new SendMessageRequest(_queueUrl, "{}")
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { _topicAttributeKey, new MessageAttributeValue { DataType = "String", StringValue = "ping" } }
            }
        });

    // Project any AWS response to its HttpStatusCode without losing the task's faulted-ness.
    private static async Task<HttpStatusCode> MapStatus<TResponse>(Task<TResponse> call) where TResponse : AmazonWebServiceResponse
        => (await call).HttpStatusCode;

    private async Task<IHealthCheckResult> RunAsync(Task<HttpStatusCode> call, HealthCheckDependency[] dependencies)
    {
        using var cts = new CancellationTokenSource();
        var completed = await Task.WhenAny(call, Task.Delay(TimeOut, cts.Token));

        if (completed != call)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object> { { "QueueUrl", _queueUrl }, { "Error", $"Timed out, {TimeOut}ms" } }, dependencies);
        }

        cts.Cancel();

        // IsFaulted, not .Result on a faulted task: reading .Result would rethrow and lose the Queue
        // dependency to the outer exception wrapper. Classify via the shared policy: an authorization
        // failure (401/403, or a known auth error code) is a persistent Failed, anything else a transient
        // Failed, enriched with the SDK error code + status, never the exception message.
        if (call.IsFaulted)
        {
            var ex = (call.Exception?.InnerException ?? call.Exception)!;
            var (errorCode, faultStatus) = AwsErrorDetails(ex);
            return HealthCheckError.Classify(Type, ex, dependencies, errorCode, faultStatus,
                new Dictionary<string, object> { { "QueueUrl", _queueUrl } });
        }

        var statusCode = call.Result;
        if (statusCode == HttpStatusCode.OK)
        {
            return HealthCheckResult.CreateInstance(true, Type,
                new Dictionary<string, object> { { "QueueUrl", _queueUrl } }, dependencies);
        }

        return HealthCheckResult.CreateInstance(false, Type,
            new Dictionary<string, object> { { "QueueUrl", _queueUrl }, { "Error", $"Returned a status of {statusCode}" } }, dependencies);
    }

    // Pulls the non-sensitive discriminators AWS already returns off an SDK exception; null for a
    // non-AWS exception (e.g. a raw connectivity failure).
    private static (string? ErrorCode, int? StatusCode) AwsErrorDetails(Exception ex)
        => ex is AmazonServiceException ase ? (ase.ErrorCode, (int)ase.StatusCode) : (null, null);

    /// <summary>The check's identifier: <c>"Sqs"</c> in reachability mode, <c>"Sqs.Active"</c> in active mode.</summary>
    public string Type => _mode == HealthCheckMode.Active ? "Sqs.Active" : "Sqs";
}
