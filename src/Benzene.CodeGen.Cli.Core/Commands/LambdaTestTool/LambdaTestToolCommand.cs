using Benzene.Clients.Aws.Lambda;
using Benzene.CodeGen.Cli.Core.Commands.Build;
using Benzene.CodeGen.Cli.Core.Commands.Spec;
using Benzene.CodeGen.Core;
using Benzene.CodeGen.LambdaTestTool;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.Extensions.Logging.Abstractions;

namespace Benzene.CodeGen.Cli.Core.Commands.LambdaTestTool;

public class LambdaTestToolCommand : CommandBase<LambdaTestToolPayload>
{
    public LambdaTestToolCommand()
        : base("lambda-test-tool", "Generates test payload JSON files (BenzeneMessage, SNS, SQS, API Gateway) for each topic of a Benzene service")
    { }

    public override async Task ExecuteAsync(LambdaTestToolPayload payload)
    {
        try
        {
            var json = await GetSpecJsonAsync(payload);
            var eventServiceDocument = new EventServiceDocumentDeserializer().Deserialize(json);

            var codeFiles = new LambdaTestFilesBuilder().BuildCodeFiles(eventServiceDocument);
            Console.WriteLine("{0} test payload files created", codeFiles.Length);

            var directory = string.IsNullOrEmpty(payload.Directory)
                ? Directory.GetCurrentDirectory()
                : payload.Directory;

            await new CodeFileWriter().CreateAsync(codeFiles, directory);
            Console.WriteLine("Completed");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }

    private static async Task<string> GetSpecJsonAsync(LambdaTestToolPayload payload)
    {
        if (!string.IsNullOrEmpty(payload.File))
        {
            return await System.IO.File.ReadAllTextAsync(payload.File);
        }

        var client = AmazonLambdaClientFactory.CreateClient(payload.Profile);
        var awsLambdaClient = new AwsLambdaSpecClient(payload.LambdaName, new AwsLambdaClient(client),
            NullLogger.Instance);
        return await awsLambdaClient.GetSpecAsync(new SpecRequest("benzene", "json"));
    }
}
