using System.Collections.Generic;
using System.Threading;
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
/// <remarks>
/// ⚠️ Side-effecting: every probe <b>really invokes</b> the target function (topic <c>ping</c>), which
/// the function must recognise and no-op. At probe cadence that is a continuous stream of real
/// invocations (cost, cold-start noise). Probe infrequently, or confirm reachability another way if you
/// don't need to actually invoke the function.
/// </remarks>
public class AwsLambdaHealthCheck : IHealthCheck
{
    private readonly AwsLambdaBenzeneMessageClient _awsLambdaBenzeneMessageClient;
    private readonly string _lambdaName;
    private const int TimeOut = 10000;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsLambdaHealthCheck"/> class.
    /// </summary>
    /// <param name="lambdaName">The name of the Lambda function to ping.</param>
    /// <param name="amazonLambda">The Lambda client used to invoke the function.</param>
    /// <param name="logger">The logger used to record invocation outcomes and failures.</param>
    public AwsLambdaHealthCheck(string lambdaName, IAmazonLambda amazonLambda, ILogger<AwsLambdaHealthCheck> logger)
    {
        _lambdaName = lambdaName;
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
        var dependencies = new[] { new HealthCheckDependency("Lambda", _lambdaName) };

        var pingLambdaTask = _awsLambdaBenzeneMessageClient.SendMessageAsync<Void, Void>("ping", null);

        using var cts = new CancellationTokenSource();
        var completed = await Task.WhenAny(pingLambdaTask, Task.Delay(TimeOut, cts.Token));

        if (completed != pingLambdaTask)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object>
                {
                    { "TimeOut", TimeOut }
                }, dependencies);
        }

        cts.Cancel();

        // IsCompletedSuccessfully, not IsCompleted: a faulted invoke is also "completed", and reading
        // .Result on it would rethrow (losing the Lambda dependency to the outer exception wrapper).
        if (pingLambdaTask.IsFaulted)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object>
                {
                    { "Error", (pingLambdaTask.Exception?.InnerException ?? pingLambdaTask.Exception)?.GetType().Name }
                }, dependencies);
        }

        if (pingLambdaTask.Result.Status == BenzeneResultStatus.Accepted)
        {
            return HealthCheckResult.CreateInstance(true, Type, new Dictionary<string, object>(), dependencies);
        }

        return HealthCheckResult.CreateInstance(false, Type, new Dictionary<string, object>
        {
            { "Status", pingLambdaTask.Result.Status }
        }, dependencies);
    }

    /// <summary>
    /// Gets the health check type identifier, <c>"Lambda"</c>.
    /// </summary>
    public string Type => "Lambda";
}
