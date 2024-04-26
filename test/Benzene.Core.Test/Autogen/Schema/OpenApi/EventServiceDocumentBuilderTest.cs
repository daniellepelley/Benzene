using Benzene.Core.Broadcast;
using Benzene.Core.MessageHandling;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

public class EventServiceDocumentBuilderTest
{
    [Fact]
    public void SerializeAndDeserializeTest()
    {
        var messageHandlerDefinition = MessageHandlerDefinition.CreateInstance("tenant:create",
            typeof(Example),
            typeof(Inner[]));

        var broadcastEventDefinition = new BroadcastEventDefinition("tenant:created", typeof(Example));
        
        var httpEndpointDefinition = new HttpEndpointDefinition("GET", "/some-route", "tenant:create");
        
        var messageSenderDefinition = MessageSenderDefinition.CreateInstance("tenant:updated", typeof(Example));

        var eventServiceDocumentBuilder = new EventServiceDocumentBuilder(new SchemaBuilder());
        var doc = eventServiceDocumentBuilder
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
            .AddMessageHandlerDefinitions(new[] { messageHandlerDefinition })
            .AddBroadcastEventDefinitions(new[] { broadcastEventDefinition })
            .AddMessageSenderDefinitions(new[] { messageSenderDefinition })
            .AddHttpEndpointDefinitions(new []{ httpEndpointDefinition }, new[] { messageHandlerDefinition })
            .Build();
            
        var json = doc.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

        var doc2 = new EventServiceDocumentDeserializer().Deserialize(json);

        var json2 = doc2.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

        Assert.Equal(json, json2);
    }
}
