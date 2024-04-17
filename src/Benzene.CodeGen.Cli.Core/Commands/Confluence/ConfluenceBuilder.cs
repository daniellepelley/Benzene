using Benzene.Clients.Aws.Lambda;
using Benzene.CodeGen.Cli.Core.Commands.Build;
using Benzene.CodeGen.Cli.Core.Commands.Spec;
using Benzene.CodeGen.Cli.Core.Confluence;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.Extensions.Logging.Abstractions;
using Benzene.CodeGen.Core;

namespace Benzene.CodeGen.Cli.Core.Commands.Confluence;

public class ConfluenceBuilder
{
    public async Task Build(ConfluencePayload payload)
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

            var confluenceClient = new ConfluenceClient(payload.ConfluenceUser, payload.ConfluenceApiToken, payload.ConfluencePage);

            foreach (var codeFile in codeFiles)
            {
                await confluenceClient.UploadFileAsync(codeFile.ToText(), $"{payload.ConfluenceAttachmentPrefix}{codeFile.Name}");
            }
            
            Console.WriteLine("Completed");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }

}
