using Benzene.Clients.Aws.Lambda;
using Benzene.CodeGen.Cli.Core.Commands.Build;
using Benzene.Schema.OpenApi;
using Microsoft.Extensions.Logging.Abstractions;

namespace Benzene.CodeGen.Cli.Core.Commands.Spec;

public class SpecCommand : CommandBase<SpecPayload>
{
    public SpecCommand()
        : base("spec", "Get schemas from a Benzene service")
    { }
    public override async Task ExecuteAsync(SpecPayload payload)
    {
        var client = AmazonLambdaClientFactory.CreateClient(payload.Profile);
        var awsLambdaClient = new AwsLambdaSpecClient(payload.LambdaName, new AwsLambdaClient(client),
            NullLogger.Instance);
        var json = await awsLambdaClient.GetSpecAsync(new SpecRequest(payload.Type, payload.Format));
        Console.WriteLine(json);
    }
}

