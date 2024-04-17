using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Benzene.HealthChecks.Core;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;

namespace Benzene.Clients.Aws.Lambda;

public class StepFunctionsHealthCheck : IHealthCheck
{
    private readonly IAmazonStepFunctions _amazonStepFunctions;
    private readonly string _stateMachineArn;
    private const int TimeOut = 10000;

    public StepFunctionsHealthCheck(string stateMachineArn, IAmazonStepFunctions amazonStepFunctions)
    {
        _stateMachineArn = stateMachineArn;
        _amazonStepFunctions = amazonStepFunctions;
    }

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

    public string Type => "StepFunctions";
}
