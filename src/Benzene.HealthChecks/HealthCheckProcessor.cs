using Benzene.Core.MessageHandling;
using Benzene.Core.Results;
using Benzene.HealthChecks.Core;
using Benzene.Results;

namespace Benzene.HealthChecks;

public static class HealthCheckProcessor
{
    public static async Task<MessageResult> PerformHealthChecksAsync(string topic, IHealthCheck[] healthChecks) 
    {
        var runningHealthChecks = healthChecks.Select(x => (x.Type, 
            new TimeOutHealthCheck(new ExceptionHandlingHealthCheck(x)).ExecuteAsync())).ToArray();
        var results = await Task.WhenAll(runningHealthChecks.Select(x => x.Item2));
        var isHealthy = results.All(x => x.Status != HealthCheckStatus.Failed);

        var healthCheckNamer = new HealthCheckNamer();
        var message = new HealthCheckResponse(isHealthy, runningHealthChecks.ToDictionary(
            x => healthCheckNamer.GetName(x.Item2.Result.Type),
            x => new HealthCheckResult(x.Item2.Result.Status, x.Item2.Result.Type, x.Item2.Result.Data)));

        return new MessageResult(topic, MessageHandlerDefinition.Empty(), ServiceResultStatus.Ok, true, message, Array.Empty<string>());
    }
}
