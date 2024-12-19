using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandling;
using Benzene.Http.Routing;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.OpenApi;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

public class OpenApiDocumentBuilderTest
{
    [Fact]
    public void SerializeAndDeserializeTest()
    {
        var messageHandlerDefinition = MessageHandlerDefinition.CreateInstance("tenant:create",
            typeof(Example),
            typeof(Inner));

        var httpEndpointDefinition = new HttpEndpointDefinition("GET", "/some-route", "tenant:create");

        var schemaBuilder = new OpenApiDocumentBuilder(new SchemaBuilder());
        var doc = schemaBuilder
            .AddInfo(new OpenApiInfo
            {
                Title = "platform-tenant-core-func",
                Version = "1.0",
                Description = "Core Tenant Data"
            })
            .AddTag(new OpenApiTag
            {
                Name = "Core Service"
            })
            .AddTag(new OpenApiTag
            {
                Name = "Platform"
            })
            .AddHttpEndpointDefinitions(new IHttpEndpointDefinition[] { httpEndpointDefinition}, new IMessageHandlerDefinition[] { messageHandlerDefinition })
            .Build();
            
        var yaml = doc.SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0);

        var doc1 = new OpenApiStringReader().Read(yaml, out _);
        var yaml1 = doc1.SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0);

        Assert.Equal(yaml, yaml1);
    }
}
