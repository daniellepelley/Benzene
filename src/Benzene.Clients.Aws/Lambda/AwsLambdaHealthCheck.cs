using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda;
using Benzene.Abstractions.Results;
using Benzene.HealthChecks.Core;
using Benzene.Results;
using Microsoft.Extensions.Logging;

namespace Benzene.Clients.Aws.Lambda;

/// <summary>
/// A health check that verifies connectivity to a Lambda function by invoking it with a "ping" message.
/// </summary>
public class AwsLambdaHealthCheck : IHealthCheck
{
    private readonly AwsLambdaBenzeneMessageClient _awsLambdaBenzeneMessageClient;
    private const int TimeOut = 10000;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsLambdaHealthCheck"/> class.
    /// </summary>
    /// <param name="lambdaName">The name of the Lambda function to ping.</param>
    /// <param name="amazonLambda">The Lambda client used to invoke the function.</param>
    /// <param name="logger">The logger used to record invocation outcomes and failures.</param>
    public AwsLambdaHealthCheck(string lambdaName, IAmazonLambda amazonLambda, ILogger<AwsLambdaHealthCheck> logger)
    {
        _awsLambdaBenzeneMessageClient = new AwsLambdaBenzeneMessageClient(lambdaName, amazonLambda, logger);
    }

    /// <summary>
    /// Invokes the target Lambda function with a "ping" message, failing if the invocation does not
    /// complete successfully within the timeout.
    /// </summary>
    /// <returns>A task that resolves to the outcome of the health check.</returns>
    /// <remarks>
    /// The ping uses <see cref="Void"/> as its response type, so
    /// <see cref="AwsLambdaBenzeneMessageClient"/> always invokes it fire-and-forget, which returns
    /// <see cref="BenzeneResultStatus.Accepted"/> on success rather than <see cref="BenzeneResultStatus.Ok"/>.
    /// </remarks>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var delay = Task.Delay(TimeOut);
        var pingLambdaTask = _awsLambdaBenzeneMessageClient.SendMessageAsync<Void, Void>("ping", null);

        await Task.WhenAny(delay, pingLambdaTask);

        if (pingLambdaTask.IsCompleted && pingLambdaTask.Result.Status == BenzeneResultStatus.Accepted)
        {
            return HealthCheckResult.CreateInstance(true, Type);
        }

        if (delay.IsCompleted)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object>
                {
                    { "TimeOut", TimeOut }
                });
        }

        return HealthCheckResult.CreateInstance(false, Type, new Dictionary<string, object>
        {
            { "Status", pingLambdaTask.Result.Status }
        });
    }

    /// <summary>
    /// Gets the health check type identifier, <c>"Lambda"</c>.
    /// </summary>
    public string Type => "Lambda";
}
