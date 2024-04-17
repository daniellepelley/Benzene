using Benzene.Clients.Aws.Lambda;
using Benzene.CodeGen.Cli.Core.Commands.Build;
using Microsoft.Extensions.Logging.Abstractions;

namespace Benzene.CodeGen.Cli.Core.Commands.HealthCheck;

public class HealthCheckCommand : CommandBase<HealthCheckPayload>
{
    public HealthCheckCommand()
        : base("healthcheck", "Runs a health check on a Benzene service")
    { }
    public override async Task ExecuteAsync(HealthCheckPayload payload)
    {
        var client = AmazonLambdaClientFactory.CreateClient(payload.Profile);
        var awsLambdaClient = new HealthCheckClient(payload.LambdaName, new AwsLambdaClient(client),
            NullLogger.Instance);
        var json = await awsLambdaClient.GetHealthCheckAsync();
        Console.Out.WriteJson(json);
    }
}

