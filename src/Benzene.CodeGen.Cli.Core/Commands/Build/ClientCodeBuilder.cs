using Benzene.Clients.Aws.Lambda;
using Benzene.CodeGen.Cli.Core.Commands.Spec;
using Benzene.CodeGen.Core;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.Extensions.Logging.Abstractions;

namespace Benzene.CodeGen.Cli.Core.Commands.Build;

public class ClientCodeBuilder : ICliCodeBuilder
{
    public async Task Build(BuildPayload payload)
    {
        try
        {
            var client = AmazonLambdaClientFactory.CreateClient(payload.Profile);
            var awsLambdaClient = new AwsLambdaSpecClient(payload.LambdaName, new AwsLambdaClient(client),
                NullLogger.Instance);
            var json = await awsLambdaClient.GetSpecAsync(new SpecRequest("benzene", "json"));

            var eventServiceDocument = new EventServiceDocumentDeserializer().Deserialize(json);

            var messageClientSdkBuilder = new CodeBuilderFactory().Create(payload);
            var codeFiles = messageClientSdkBuilder.BuildCodeFiles(eventServiceDocument);
            Console.WriteLine("{0} code files created", codeFiles.Length);

            var writer = new CodeFileWriter();

            var directory = string.IsNullOrEmpty(payload.Directory)
                ? Directory.GetCurrentDirectory()
                : payload.Directory;
            
            await writer.CreateAsync(codeFiles, directory);
            Console.WriteLine("Completed");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }

}
