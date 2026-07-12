using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Benzene.HealthChecks.Core;

namespace Benzene.Clients.Aws.StepFunctions;

/// <summary>
/// Checks the health of a Step Functions state machine by starting an execution with an empty input
/// and confirming it's accepted within a timeout.
/// </summary>
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
        var delay = Task.Delay(TimeOut);
        var pingState = _amazonStepFunctions.StartExecutionAsync(new StartExecutionRequest
        {
            StateMachineArn = _stateMachineArn,
            Input = "{}"
        });

        await Task.WhenAny(delay, pingState);

        if (pingState.IsCompleted && pingState.Result.HttpStatusCode == HttpStatusCode.OK)
        {
            return HealthCheckResult.CreateInstance(true, Type,
                new Dictionary<string, object>
                {
                    { "StateMachineArn", _stateMachineArn },
                });
        }
        if (delay.IsCompleted)
        {
            return HealthCheckResult.CreateInstance(false, Type,
                new Dictionary<string, object>
                {
                    { "StateMachineArn", _stateMachineArn },
                    { "Error", $"Timed out, {TimeOut}ms" }
                });
        }
        return HealthCheckResult.CreateInstance(false, Type, new Dictionary<string, object>
        {
            { "Error", $"Returned a status of {pingState.Result.HttpStatusCode}" },
            { "StateMachineArn", _stateMachineArn }
        });
    }

    /// <summary>
    /// Gets the health check type name, <c>"StepFunctions"</c>.
    /// </summary>
    public string Type => "StepFunctions";
}
