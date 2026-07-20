using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.StepFunctions;

/// <summary>
/// Checks the health of a Step Functions state machine by starting an execution with an empty input
/// and confirming it's accepted within a timeout.
/// </summary>
/// <remarks>
/// ⚠️ Side-effecting: every probe <b>starts a real execution</b> of the state machine. At a typical
/// Kubernetes/LB probe cadence (e.g. every 10s) that is a continuous stream of real executions - cost,
/// noise, and history-retention pressure. Point it at a cheap no-op state machine, probe it
/// infrequently, or prefer a read-only reachability check (e.g. <c>DescribeStateMachine</c>) if you
/// only need to confirm the service/ARN is reachable rather than executable.
/// </remarks>
public class StepFunctionsHealthCheck : IHealthCheck
{
    private readonly IAmazonStepFunctions _amazonStepFunctions;
    private readonly string _stateMachineArn;
    private const int TimeOut = 10000;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepFunctionsHealthCheck"/> class.
    /// </summary>
    /// <param name="stateMachineArn">The ARN of the state machine to check.</param>
    /// <param name="amazonStepFunctions">The Step Functions client used to start the execution.</param>
    public StepFunctionsHealthCheck(string stateMachineArn, IAmazonStepFunctions amazonStepFunctions)
    {
        _stateMachineArn = stateMachineArn;
        _amazonStepFunctions = amazonStepFunctions;
    }

    /// <summary>
    /// Starts an execution of the state machine and reports success, timeout, or failure.
    /// </summary>
    /// <returns>
    /// A task that resolves to the health check result: successful if the execution starts within
    /// <see cref="TimeOut"/> milliseconds and returns an HTTP 200; a timeout result if it doesn't
    /// complete in time; otherwise a failure result with the returned status code.
    /// </returns>
    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var dependencies = new[] { new HealthCheckDependency("StateMachine", _stateMachineArn) };

        var pingState = _amazonStepFunctions.StartExecutionAsync(new StartExecutionRequest
        {
            StateMachineArn = _stateMachineArn,
            Input = "{}"
        });

        using var cts = new CancellationTokenSource();
        var completed = await Task.WhenAny(pingState, Task.Delay(TimeOut, cts.Token));

        if (completed != pingState)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object>
                {
                    { "StateMachineArn", _stateMachineArn },
                    { "Error", $"Timed out, {TimeOut}ms" }
                }, dependencies);
        }

        cts.Cancel();

        // IsCompletedSuccessfully, not IsCompleted: a faulted start is also "completed", and reading
        // .Result on it would rethrow (losing the StateMachine dependency to the outer exception wrapper).
        if (pingState.IsFaulted)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object>
                {
                    { "StateMachineArn", _stateMachineArn },
                    { "Error", (pingState.Exception?.InnerException ?? pingState.Exception)?.GetType().Name }
                }, dependencies);
        }

        var statusCode = pingState.Result.HttpStatusCode;
        if (statusCode == HttpStatusCode.OK)
        {
            return HealthCheckResult.CreateInstance(true, Type,
                new Dictionary<string, object> { { "StateMachineArn", _stateMachineArn } }, dependencies);
        }

        return HealthCheckResult.CreateInstance(false, Type, new Dictionary<string, object>
        {
            { "StateMachineArn", _stateMachineArn },
            { "Error", $"Returned a status of {statusCode}" }
        }, dependencies);
    }

    /// <summary>
    /// Gets the health check type name, <c>"StepFunctions"</c>.
    /// </summary>
    public string Type => "StepFunctions";
}
