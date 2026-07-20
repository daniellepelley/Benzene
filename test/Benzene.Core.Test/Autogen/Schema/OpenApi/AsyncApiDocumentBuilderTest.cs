using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.ResponseEvents;
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

    [Fact]
    public void Operations_UseTheCorrectAsyncApiPerspective()
    {
        // AsyncAPI 2.x is application-centric and counter-intuitive: `publish` = messages the app
        // RECEIVES, `subscribe` = messages the app SENDS. A handler receives its request and sends its
        // reply; a broadcast event and an egress message-sender are both things the app sends.
        var handler = MessageHandlerDefinition.CreateInstance("tenant:create", typeof(Example), typeof(Inner));
        var broadcast = new ResponseEventDefinition("tenant:created", typeof(Example));
        var sender = MessageSenderDefinition.CreateInstance("tenant:updated", typeof(Example));

        var doc = new AsyncApiDocumentBuilder(new SchemaBuilder())
            .AddInfo(new AsyncApiInfo { Title = "tenant-core", Version = "1.0" })
            .AddMessageHandlerDefinitions(new[] { handler })
            .AddBroadcastEventDefinitions(new[] { broadcast })
            .AddMessageSenderDefinitions(new[] { sender })
            .Build();

        // Handler request is received ⇒ publish (not subscribe); reply is sent ⇒ subscribe.
        Assert.NotNull(doc.Channels["tenant:create"].Publish);
        Assert.Null(doc.Channels["tenant:create"].Subscribe);
        Assert.NotNull(doc.Channels["tenant:create:benzeneResult"].Subscribe);
        Assert.Null(doc.Channels["tenant:create:benzeneResult"].Publish);

        // Broadcast event and egress sender are produced/sent ⇒ subscribe (not publish).
        Assert.NotNull(doc.Channels["tenant:created"].Subscribe);
        Assert.Null(doc.Channels["tenant:created"].Publish);
        Assert.NotNull(doc.Channels["tenant:updated"].Subscribe);
        Assert.Null(doc.Channels["tenant:updated"].Publish);

        // Document-root metadata is populated (no placeholder junk on the operations).
        Assert.Equal("application/json", doc.DefaultContentType);
        Assert.False(string.IsNullOrEmpty(doc.Id));
        Assert.Null(doc.Channels["tenant:created"].Subscribe.Summary);
        Assert.Null(doc.Channels["tenant:created"].Subscribe.Description);
    }
}

