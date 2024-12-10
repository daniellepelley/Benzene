using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda;
using Benzene.Abstractions.Logging;
using Benzene.HealthChecks.Core;
using Benzene.Results;

namespace Benzene.Clients.Aws.Lambda;

public class AwsLambdaHealthCheck : IHealthCheck
{
    private readonly AwsLambdaBenzeneMessageClient _awsLambdaBenzeneMessageClient;
    private const int TimeOut = 10000;

    public AwsLambdaHealthCheck(string lambdaName, IAmazonLambda amazonLambda, IBenzeneLogger logger)
    {
        _awsLambdaBenzeneMessageClient = new AwsLambdaBenzeneMessageClient(lambdaName, amazonLambda, logger);
    }

    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var delay = Task.Delay(TimeOut);
        var pingLambdaTask = _awsLambdaBenzeneMessageClient.SendMessageAsync<Void, Void>("ping", null);

        await Task.WhenAny(delay, pingLambdaTask);

        if (pingLambdaTask.IsCompleted && pingLambdaTask.Result.Status == ClientResultStatus.Ok)
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

    public string Type => "Lambda";
}
