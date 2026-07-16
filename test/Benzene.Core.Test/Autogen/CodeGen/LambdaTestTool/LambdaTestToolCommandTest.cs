using System;
using System.IO;
using System.Threading.Tasks;
using Benzene.CodeGen.Cli.Core.Commands.LambdaTestTool;
using Benzene.Core.MessageHandlers;
using Benzene.Http.Routing;
using Benzene.Schema.OpenApi.EventService;
using Benzene.Test.Autogen.CodeGen.Model;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.LambdaTestTool;

public class LambdaTestToolCommandTest
{
    [Fact]
    public async Task ExecuteAsync_SpecFromFile_WritesTestPayloadFiles()
    {
        var messageHandlerDefinition = MessageHandlerDefinition.CreateInstance("tenant:create",
            typeof(CreateTenantMessage),
            typeof(TenantDto));

        var httpEndpointDefinition = HttpEndpointDefinition.CreateInstance("POST", "/tenants", "tenant:create");

        var eventServiceDocument = httpEndpointDefinition.ToEventServiceDocument(messageHandlerDefinition);
        var json = eventServiceDocument.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

        var specPath = Path.Combine(Path.GetTempPath(), $"benzene-spec-{Guid.NewGuid():N}.json");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"benzene-test-tool-{Guid.NewGuid():N}");
        try
        {
            await File.WriteAllTextAsync(specPath, json);

            await new LambdaTestToolCommand().ExecuteAsync(new LambdaTestToolPayload
            {
                File = specPath,
                Directory = outputDirectory
            });

            var files = Directory.GetFiles(outputDirectory);
            Assert.Equal(4, files.Length);
            Assert.Contains(files, x => x.EndsWith("tenant-create-benzene-message.json"));
            Assert.Contains(files, x => x.EndsWith("tenant-create-sns.json"));
            Assert.Contains(files, x => x.EndsWith("tenant-create-sqs.json"));
            Assert.Contains(files, x => x.EndsWith("tenant-create-api-gateway.json"));
        }
        finally
        {
            File.Delete(specPath);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }
    }
}
