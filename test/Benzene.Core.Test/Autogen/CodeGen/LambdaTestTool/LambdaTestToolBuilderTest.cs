using System.Collections.Generic;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.Sns.TestHelpers;
using Benzene.Aws.Lambda.Sqs.TestHelpers;
using Benzene.CodeGen.Core;
using Benzene.CodeGen.LambdaTestTool;
using Benzene.CodeGen.Markdown;
using Benzene.Core.MessageHandlers;
using Benzene.Http.Routing;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Benzene.Test.Autogen.CodeGen.Model;
using Benzene.Test.Autogen.Schema.OpenApi;
using Benzene.Testing;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.LambdaTestTool;

public class LambdaTestToolBuilderTest
{
    [Fact]
    public void SerializeAndDeserializeTest()
    {
        var messageHandlerDefinition = MessageHandlerDefinition.CreateInstance("tenant:create",
            typeof(InternalDto),
            typeof(Inner));

        var httpEndpointDefinition = HttpEndpointDefinition.CreateInstance("POST", "/tenants", "tenant:create");

        var eventServiceDocument = httpEndpointDefinition.ToEventServiceDocument(messageHandlerDefinition);
        
        var knownValues = new Dictionary<string, object>();
        
        var eventBuilders = new IExampleBuilder[]
        {
            new BenzeneMessageExampleBuilder(knownValues),
            new ExampleBuilder("sns", (topic, payload) => MessageBuilder.Create(topic, payload).AsSns(), knownValues),
            new ExampleBuilder("sqs", (topic, payload) => MessageBuilder.Create(topic, payload).AsSqs(), knownValues),
            new HttpExampleBuilder("api-gateway", (method, path, payload) => HttpBuilder.Create(method, path, payload).AsApiGatewayRequest(), knownValues)
        };

        var builder = new LambdaTestFilesBuilder(eventBuilders);
        var codeFiles = builder.BuildCodeFiles(eventServiceDocument);
        Assert.Equal(4, codeFiles.Length);
    }
    
        [Fact]
    public void SerializeAndDeserialize_WithKnownValues_Test()
    {
        var messageHandlerDefinition = MessageHandlerDefinition.CreateInstance("tenant:create",
            typeof(Example),
            typeof(Inner));
    
        var eventServiceDocument = messageHandlerDefinition.ToEventServiceDocument();
        
        var knownValues = new Dictionary<string, object>
        {
            { "title", "some-title" }
        };

        var eventBuilders = new IExampleBuilder[]
        {
            new BenzeneMessageExampleBuilder(knownValues),
            new ExampleBuilder("sns", (topic, payload) => MessageBuilder.Create(topic, payload).AsSns(), knownValues),
            new ExampleBuilder("sqs", (topic, payload) => MessageBuilder.Create(topic, payload).AsSqs(), knownValues),
            new HttpExampleBuilder("api-gateway", (method, path, payload) => HttpBuilder.Create(method, path, payload).AsApiGatewayRequest(), knownValues)
        };
    
        var builder = new LambdaTestFilesBuilder(eventBuilders);
        var codeFiles = builder.BuildCodeFiles(eventServiceDocument);
        Assert.Equal(3, codeFiles.Length);
    }

}
