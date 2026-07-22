using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.StepFunctions;

/// <summary>
/// Verifies a Step Functions state machine. In the default <see cref="HealthCheckMode.Reachability"/>
/// mode this is a <b>non-destructive</b> read-only <c>DescribeStateMachine</c> call; in
/// <see cref="HealthCheckMode.Active"/> mode it starts a real execution (side-effecting — a continuous
/// stream of real executions at probe cadence: cost, noise, history-retention pressure).
/// </summary>
/// <remarks>
/// The reachability check proves the state machine exists, is reachable, and the credentials can read it
/// (<c>states:DescribeStateMachine</c>) — not that a start would succeed (<c>states:StartExecution</c> is
/// a different permission). Use <see cref="HealthCheckMode.Active"/> only when you must exercise the
/// start path (point it at a cheap no-op state machine), and keep it off a frequent poll and off probes.
/// </remarks>
public class StepFunctionsHealthCheck : IHealthCheck
{
    private readonly IAmazonStepFunctions _amazonStepFunctions;
    private readonly string _stateMachineArn;
    private readonly HealthCheckMode _mode;
    private const int TimeOut = 10000;

    /// <summary>Initializes a new instance of the <see cref="StepFunctionsHealthCheck"/> class.</summary>
    /// <param name="stateMachineArn">The ARN of the state machine to check.</param>
    /// <param name="amazonStepFunctions">The Step Functions client used to run the check.</param>
    /// <param name="mode">Reachability (default, read-only) or Active (starts an execution — side-effecting).</param>
    public StepFunctionsHealthCheck(string stateMachineArn, IAmazonStepFunctions amazonStepFunctions,
        HealthCheckMode mode = HealthCheckMode.Reachability)
    {
        _stateMachineArn = stateMachineArn;
        _amazonStepFunctions = amazonStepFunctions;
        _mode = mode;
    }

    /// <summary>Runs the check and reports the outcome.</summary>
    public Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("StateMachine", _stateMachineArn) };

        var call = _mode == HealthCheckMode.Active
            ? MapStatus(_amazonStepFunctions.StartExecutionAsync(new StartExecutionRequest
            {
                StateMachineArn = _stateMachineArn,
                Input = "{}"
            }))
            : MapStatus(_amazonStepFunctions.DescribeStateMachineAsync(new DescribeStateMachineRequest
            {
                StateMachineArn = _stateMachineArn
            }));

        return RunAsync(call, dependencies);
    }

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
                new Dictionary<string, object> { { "StateMachineArn", _stateMachineArn }, { "Error", $"Timed out, {TimeOut}ms" } }, dependencies);
        }

        cts.Cancel();

        // IsFaulted, not .Result on a faulted task: reading .Result would rethrow and lose the
        // StateMachine dependency to the outer exception wrapper. Report the failure type, never the message.
        if (call.IsFaulted)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object>
                {
                    { "StateMachineArn", _stateMachineArn },
                    { "Error", (call.Exception?.InnerException ?? call.Exception)?.GetType().Name }
                }, dependencies);
        }

        var statusCode = call.Result;
        if (statusCode == HttpStatusCode.OK)
        {
            return HealthCheckResult.CreateInstance(true, Type,
                new Dictionary<string, object> { { "StateMachineArn", _stateMachineArn } }, dependencies);
        }

        return HealthCheckResult.CreateInstance(false, Type,
            new Dictionary<string, object> { { "StateMachineArn", _stateMachineArn }, { "Error", $"Returned a status of {statusCode}" } }, dependencies);
    }

    /// <summary>The check's identifier: <c>"StepFunctions"</c> in reachability mode, <c>"StepFunctions.Active"</c> in active mode.</summary>
    public string Type => _mode == HealthCheckMode.Active ? "StepFunctions.Active" : "StepFunctions";
}
