using Benzene.Abstractions.MessageHandlers.Info;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.Info;
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
    public void Build_FlagsReservedUtilityTopics_ButNotDomainTopics()
    {
        var domain = MessageHandlerDefinition.CreateInstance("tenant:create", typeof(Example), typeof(Inner));
        var spec = MessageHandlerDefinition.CreateInstance("spec", typeof(Example), typeof(Inner));
        var health = MessageHandlerDefinition.CreateInstance("healthcheck", typeof(Example), typeof(Inner));

        var doc = new EventServiceDocumentBuilder(new SchemaBuilder())
            .AddMessageHandlerDefinitions(new[] { domain, spec, health })
            .Build();

        Assert.False(Assert.Single(doc.Requests, x => x.Topic == "tenant:create").Reserved);
        Assert.True(Assert.Single(doc.Requests, x => x.Topic == "spec").Reserved);
        Assert.True(Assert.Single(doc.Requests, x => x.Topic == "healthcheck").Reserved);

        var json = doc.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
        Assert.Contains("\"reserved\": true", json);

        // Round-trips through the deserializer (JsonProperty("reserved")).
        var roundTripped = new EventServiceDocumentDeserializer().Deserialize(json);
        Assert.True(Assert.Single(roundTripped.Requests, x => x.Topic == "spec").Reserved);
        Assert.False(Assert.Single(roundTripped.Requests, x => x.Topic == "tenant:create").Reserved);
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

    [Fact]
    public void Build_WritesVersion_ForVersionedRequestsAndEvents_ButOmitsItWhenUnversioned()
    {
        var versionedRequest = MessageHandlerDefinition.CreateInstance("shipping:booked", "v2", typeof(Example), typeof(Inner), typeof(object));
        var unversionedRequest = MessageHandlerDefinition.CreateInstance("tenant:create", typeof(Example), typeof(Inner));
        var versionedEvent = new BroadcastEventDefinition(new Topic("shipping:booked", "v2"), typeof(Example));

        var doc = new EventServiceDocumentBuilder(new SchemaBuilder())
            .AddMessageHandlerDefinitions(new[] { versionedRequest, unversionedRequest })
            .AddBroadcastEventDefinitions(new[] { versionedEvent })
            .Build();

        Assert.Equal("v2", Assert.Single(doc.Requests, x => x.Topic == "shipping:booked").Version);
        Assert.Equal(string.Empty, Assert.Single(doc.Requests, x => x.Topic == "tenant:create").Version);
        Assert.Equal("v2", Assert.Single(doc.Events, x => x.Topic == "shipping:booked").Version);

        var json = doc.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
        Assert.Contains("\"version\": \"v2\"", json);

        // Round-trips through the deserializer.
        var roundTripped = new EventServiceDocumentDeserializer().Deserialize(json);
        Assert.Equal("v2", Assert.Single(roundTripped.Requests, x => x.Topic == "shipping:booked").Version);
        Assert.Equal(string.Empty, Assert.Single(roundTripped.Requests, x => x.Topic == "tenant:create").Version);
        Assert.Equal("v2", Assert.Single(roundTripped.Events, x => x.Topic == "shipping:booked").Version);
    }

    [Fact]
    public void Build_WritesTransports_WhenAnyAreRegistered_ButOmitsTheFieldWhenNone()
    {
        var transportsInfo = new TransportsInfo(new ITransportInfo[] { new TransportInfo("sqs"), new TransportInfo("http") });

        var doc = new EventServiceDocumentBuilder(new SchemaBuilder())
            .AddTransportsInfo(transportsInfo)
            .Build();

        Assert.Equal(new[] { "sqs", "http" }, doc.Transports);

        var json = doc.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
        Assert.Contains("\"transports\"", json);

        var roundTripped = new EventServiceDocumentDeserializer().Deserialize(json);
        Assert.Equal(new[] { "sqs", "http" }, roundTripped.Transports);

        var docWithNoTransports = new EventServiceDocumentBuilder(new SchemaBuilder()).Build();
        Assert.Empty(docWithNoTransports.Transports);

        var jsonWithNoTransports = docWithNoTransports.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
        Assert.DoesNotContain("\"transports\"", jsonWithNoTransports);

        var roundTrippedWithNoTransports = new EventServiceDocumentDeserializer().Deserialize(jsonWithNoTransports);
        Assert.Empty(roundTrippedWithNoTransports.Transports);
    }
}

