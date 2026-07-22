using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Runtime;
using Benzene.Abstractions.Results;
using Benzene.HealthChecks.Core;
using Benzene.Results;
using Microsoft.Extensions.Logging;
using Void = Benzene.Abstractions.Results.Void;

namespace Benzene.Clients.Aws.Lambda;

/// <summary>
/// Verifies connectivity to a Lambda function. In the default <see cref="HealthCheckMode.Reachability"/>
/// mode this is a <b>non-destructive</b> read-only <c>GetFunctionConfiguration</c> call; in
/// <see cref="HealthCheckMode.Active"/> mode it really invokes the function with a <c>ping</c> message
/// (side-effecting — cost + cold-start noise at probe cadence, and the function must no-op it).
/// </summary>
/// <remarks>
/// The reachability check proves the function exists, is reachable, and the credentials can read it
/// (<c>lambda:GetFunctionConfiguration</c>) — not that an invoke would succeed
/// (<c>lambda:InvokeFunction</c> is a different permission). Use <see cref="HealthCheckMode.Active"/> only
/// when you need to exercise the invoke path, and keep it off a frequent poll and off liveness/readiness.
/// </remarks>
public class AwsLambdaHealthCheck : IHealthCheck
{
    private readonly IAmazonLambda _amazonLambda;
    private readonly AwsLambdaBenzeneMessageClient _awsLambdaBenzeneMessageClient;
    private readonly string _lambdaName;
    private readonly HealthCheckMode _mode;
    private const int TimeOut = 10000;

    /// <summary>Initializes a new instance of the <see cref="AwsLambdaHealthCheck"/> class.</summary>
    /// <param name="lambdaName">The name of the Lambda function to check.</param>
    /// <param name="amazonLambda">The Lambda client used to run the check.</param>
    /// <param name="logger">The logger used by the active-mode invoke path.</param>
    /// <param name="mode">Reachability (default, read-only) or Active (invokes the function — side-effecting).</param>
    public AwsLambdaHealthCheck(string lambdaName, IAmazonLambda amazonLambda, ILogger<AwsLambdaHealthCheck> logger,
        HealthCheckMode mode = HealthCheckMode.Reachability)
    {
        _lambdaName = lambdaName;
        _amazonLambda = amazonLambda;
        _mode = mode;
        _awsLambdaBenzeneMessageClient = new AwsLambdaBenzeneMessageClient(lambdaName, amazonLambda, logger);
    }

    /// <summary>Runs the check and reports the outcome.</summary>
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("Lambda", _lambdaName) };

        return _mode == HealthCheckMode.Active
            ? RunAsync(_awsLambdaBenzeneMessageClient.SendMessageAsync<Void, Void>("ping", null),
                r => r.Status == BenzeneResultStatus.Accepted, r => ("Status", (object)r.Status), dependencies)
            : RunAsync(_amazonLambda.GetFunctionConfigurationAsync(_lambdaName),
                r => r.HttpStatusCode == HttpStatusCode.OK, r => ("Status", (object)r.HttpStatusCode), dependencies);
    }

    private async Task<IHealthCheckResult> RunAsync<T>(Task<T> call, Func<T, bool> isHealthy,
        Func<T, (string Key, object Value)> failInfo, HealthCheckDependency[] dependencies)
    {
        using var cts = new CancellationTokenSource();
        var completed = await Task.WhenAny(call, Task.Delay(TimeOut, cts.Token));

        if (completed != call)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object> { { "TimeOut", TimeOut } }, dependencies);
        }

        cts.Cancel();

        // IsFaulted, not .Result on a faulted task: reading .Result would rethrow and lose the Lambda
        // dependency to the outer exception wrapper. Classify via the shared policy: 401/403 -> Warning,
        // else Failed, enriched with the SDK error code + status, never the exception message.
        if (call.IsFaulted)
        {
            var ex = (call.Exception?.InnerException ?? call.Exception)!;
            var (errorCode, statusCode) = AwsErrorDetails(ex);
            return HealthCheckError.Classify(Type, ex, dependencies, errorCode, statusCode);
        }

        var result = call.Result;
        if (isHealthy(result))
        {
            return HealthCheckResult.CreateInstance(true, Type, new Dictionary<string, object>(), dependencies);
        }

        var (key, value) = failInfo(result);
        return HealthCheckResult.CreateInstance(false, Type,
            new Dictionary<string, object> { { key, value } }, dependencies);
    }

    // Pulls the non-sensitive discriminators AWS already returns off an SDK exception; null for a
    // non-AWS exception (e.g. a raw connectivity failure).
    private static (string? ErrorCode, int? StatusCode) AwsErrorDetails(Exception ex)
        => ex is AmazonServiceException ase ? (ase.ErrorCode, (int)ase.StatusCode) : (null, null);

    /// <summary>The check's identifier: <c>"Lambda"</c> in reachability mode, <c>"Lambda.Active"</c> in active mode.</summary>
    public string Type => _mode == HealthCheckMode.Active ? "Lambda.Active" : "Lambda";
}
