using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// Verifies connectivity to an SNS topic with a read-only <c>GetTopicAttributes</c> call - the SNS
/// analogue of <c>SqsHealthCheck</c>'s queue check, but non-side-effecting (it does not publish).
/// A permission error (e.g. missing <c>sns:GetTopicAttributes</c>) is reported as a
/// <see cref="HealthCheckStatus.Warning"/>, not a failure (§3.9), and the SDK's error code + HTTP status
/// are surfaced in <c>Data</c> (never the exception message).
/// </summary>
public class SnsHealthCheck : IHealthCheck
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly string _topicArn;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="topicArn">The ARN of the topic to check.</param>
    /// <param name="sns">The SNS client used to read the topic's attributes.</param>
    public SnsHealthCheck(string topicArn, IAmazonSimpleNotificationService sns)
    {
        _topicArn = topicArn;
        _sns = sns;
    }

    /// <inheritdoc />
    public string Type => "Sns";

    /// <inheritdoc />
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("Topic", _topicArn) };

        try
        {
            var response = await _sns.GetTopicAttributesAsync(_topicArn);
            return HealthCheckResult.CreateInstance(response.HttpStatusCode == HttpStatusCode.OK, Type,
                new Dictionary<string, object> { { "TopicArn", _topicArn } }, dependencies);
        }
        catch (Exception ex)
        {
            // Expected failures (topic missing, no connectivity, no permission) are a classified result,
            // not a throw. HealthCheckError applies the shared policy: 401/403 -> Warning, else Failed,
            // enriched with the SDK's error code + status, never the exception message.
            var (errorCode, statusCode) = AwsErrorDetails(ex);
            return HealthCheckError.Classify(Type, ex, dependencies, errorCode, statusCode,
                new Dictionary<string, object> { { "TopicArn", _topicArn } });
        }
    }

    // Pulls the non-sensitive discriminators AWS already returns off an SDK exception; null for a
    // non-AWS exception (e.g. a raw connectivity failure).
    private static (string? ErrorCode, int? StatusCode) AwsErrorDetails(Exception ex)
        => ex is AmazonServiceException ase ? (ase.ErrorCode, (int)ase.StatusCode) : (null, null);
}
