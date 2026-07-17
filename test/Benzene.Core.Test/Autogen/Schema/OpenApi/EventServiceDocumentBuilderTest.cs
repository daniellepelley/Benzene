using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Extras.Broadcast;
using Benzene.Http.Routing;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.EventService;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
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
                Title = "benzene-tenant-core-func",
                Version = "1.0",
                Description = "Core Tenant Data"
            })
            .AddTag(new OpenApiTag
            {
                Name = "Core Service"
            })
            .AddTag(new OpenApiTag
            {
                Name = "benzene"
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

    [Fact]
    public void Build_GeneratesExamplesForRequestsAndEvents()
    {
        var messageHandlerDefinition = MessageHandlerDefinition.CreateInstance("tenant:create",
            typeof(Example),
            typeof(Inner));

        var broadcastEventDefinition = new BroadcastEventDefinition("tenant:created", typeof(Inner));

        var doc = new EventServiceDocumentBuilder(new SchemaBuilder())
            .AddMessageHandlerDefinitions(new[] { messageHandlerDefinition })
            .AddBroadcastEventDefinitions(new[] { broadcastEventDefinition })
            .Build();

        var requestExample = Assert.IsType<OpenApiObject>(doc.Requests[0].Example);
        Assert.Equal("value", Assert.IsType<OpenApiString>(requestExample["title"]).Value);
        var innerArray = Assert.IsType<OpenApiArray>(requestExample["inner"]);
        var innerExample = Assert.IsType<OpenApiObject>(innerArray[0]);
        Assert.Equal("2023-01-01T12:00:00.000Z", Assert.IsType<OpenApiString>(innerExample["date"]).Value);

        var eventExample = Assert.IsType<OpenApiObject>(doc.Events[0].Example);
        Assert.Equal("value", Assert.IsType<OpenApiString>(eventExample["title"]).Value);

        var json = doc.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
        Assert.Contains("\"example\"", json);
    }
}

