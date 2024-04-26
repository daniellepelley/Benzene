using Benzene.Core.Broadcast;
using Benzene.Core.MessageHandling;
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

        var broadcastEventDefinition = new BroadcastEventDefinition("tenant:created", typeof(Example));
        
        var messageSenderDefinition = MessageSenderDefinition.CreateInstance("tenant:updated", typeof(Example));

        var asyncApiDocumentBuilder = new AsyncApiDocumentBuilder(new SchemaBuilder());
        var doc = asyncApiDocumentBuilder
            .AddInfo(new AsyncApiInfo
            {
                Title = "platform-tenant-core-func",
                Version = "1.0",
                Description = "Core Tenant Data"
            })
            .AddTag(new AsyncApiTag
            {
                Name = "Core Service"
            })
            .AddTag(new AsyncApiTag
            {
                Name = "Platform"
            })
            .AddMessageHandlerDefinitions(new[] { messageHandlerDefinition })
            .AddBroadcastEventDefinitions(new[] { broadcastEventDefinition })
            .AddMessageSenderDefinitions(new[] { messageSenderDefinition })
            .Build();
            
        var yaml = doc.SerializeAsYaml(AsyncApiVersion.AsyncApi2_0);

        var doc1 = new AsyncApiStringReader().Read(yaml, out _);
        var yaml1 = doc1.SerializeAsYaml(AsyncApiVersion.AsyncApi2_0);

        Assert.Equal(yaml, yaml1);

    }
}
