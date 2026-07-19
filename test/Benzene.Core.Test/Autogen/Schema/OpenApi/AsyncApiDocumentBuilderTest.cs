using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Extras.ResponseEvents;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.AsyncApi;
using LEGO.AsyncAPI;
using LEGO.AsyncAPI.Models;
using LEGO.AsyncAPI.Readers;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

public class AsyncApiDocumentBuilderTest
{
    [Fact]
    public void SerializeAndDeserializeTest()
    {
        var messageHandlerDefinition = MessageHandlerDefinition.CreateInstance("tenant:create",
            typeof(Example),
            typeof(Inner));

        var responseEventDefinition = new ResponseEventDefinition("tenant:created", typeof(Example));
        
        var messageSenderDefinition = MessageSenderDefinition.CreateInstance("tenant:updated", typeof(Example));

        var asyncApiDocumentBuilder = new AsyncApiDocumentBuilder(new SchemaBuilder());
        var doc = asyncApiDocumentBuilder
            .AddInfo(new AsyncApiInfo
            {
                Title = "benzene-tenant-core-func",
                Version = "1.0",
                Description = "Core Tenant Data"
            })
            .AddTag(new AsyncApiTag
            {
                Name = "Core Service"
            })
            .AddTag(new AsyncApiTag
            {
                Name = "benzene"
            })
            .AddMessageHandlerDefinitions(new[] { messageHandlerDefinition })
            .AddBroadcastEventDefinitions(new[] { responseEventDefinition })
            .AddMessageSenderDefinitions(new[] { messageSenderDefinition })
            .Build();
            
        var yaml = doc.SerializeAsYaml(AsyncApiVersion.AsyncApi2_0);

        var doc1 = new AsyncApiStringReader().Read(yaml, out _);
        var yaml1 = doc1.SerializeAsYaml(AsyncApiVersion.AsyncApi2_0);

        Assert.Equal(yaml, yaml1);

    }
}

