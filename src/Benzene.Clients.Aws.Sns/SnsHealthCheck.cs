using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.Sns;

/// <summary>
/// Verifies connectivity to an SNS topic with a read-only <c>GetTopicAttributes</c> call - the SNS
/// analogue of <c>SqsHealthCheck</c>'s queue check, but non-side-effecting (it does not publish).
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
            // Expected failures (topic missing, no connectivity) are a failed result, not a throw;
            // report the exception type, never its message.
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object> { { "TopicArn", _topicArn }, { "Error", ex.GetType().Name } },
                dependencies);
        }
    }
}
