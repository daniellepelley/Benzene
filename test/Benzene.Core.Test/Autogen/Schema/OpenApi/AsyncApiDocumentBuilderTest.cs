using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.ResponseEvents;
using Benzene.Schema.OpenApi;
using Benzene.Schema.OpenApi.AsyncApi;
using ByteBard.AsyncAPI;
using ByteBard.AsyncAPI.Models;
using ByteBard.AsyncAPI.Readers;
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
            
        var yaml = doc.SerializeAsYaml(AsyncApiVersion.AsyncApi3_0);

        // Round-trips structurally (ByteBard re-emits component schemas with an explicit schemaFormat
        // wrapper on read-back, so a byte-for-byte comparison isn't meaningful; the shape is).
        var doc1 = new AsyncApiStringReader().Read(yaml, out _);

        Assert.Equal(doc.Channels.Count, doc1.Channels.Count);
        Assert.Equal(doc.Operations.Count, doc1.Operations.Count);
        Assert.Equal(doc.Components.Schemas.Count, doc1.Components.Schemas.Count);
    }

    [Fact]
    public void Operations_UseTheCorrectAsyncApiPerspective()
    {
        // AsyncAPI 3.0 names operations from the application's perspective with `action`: a handler
        // RECEIVES its request (and its reply is modelled with the native `reply` object); a broadcast
        // event and an egress message-sender are both things the app SENDS. Channel/operation map keys
        // are sanitized to ^[A-Za-z0-9.\-_]+$ (the topic itself lives in the channel's `address`).
        var handler = MessageHandlerDefinition.CreateInstance("tenant:create", typeof(Example), typeof(Inner));
        var broadcast = new ResponseEventDefinition("tenant:created", typeof(Example));
        var sender = MessageSenderDefinition.CreateInstance("tenant:updated", typeof(Example));

        var doc = new AsyncApiDocumentBuilder(new SchemaBuilder())
            .AddInfo(new AsyncApiInfo { Title = "tenant-core", Version = "1.0" })
            .AddMessageHandlerDefinitions(new[] { handler })
            .AddBroadcastEventDefinitions(new[] { broadcast })
            .AddMessageSenderDefinitions(new[] { sender })
            .Build();

        // The handler receives its request and replies via the native reply object. (The topic itself
        // lives in the channel's `address`; the operation's channel is a $ref only resolved by a reader.)
        var handle = doc.Operations["tenant_create"];
        Assert.Equal(AsyncApiAction.Receive, handle.Action);
        Assert.NotNull(handle.Reply);
        Assert.Equal("tenant:create", doc.Channels["tenant_create"].Address);
        Assert.Equal("tenant:create:benzeneResult", doc.Channels["tenant_create_benzeneResult"].Address);

        // Broadcast event and egress sender are produced/sent.
        Assert.Equal(AsyncApiAction.Send, doc.Operations["tenant_created"].Action);
        Assert.Null(doc.Operations["tenant_created"].Reply);
        Assert.Equal(AsyncApiAction.Send, doc.Operations["tenant_updated"].Action);

        // Document-root metadata is populated, and it's an AsyncAPI 3.x document.
        Assert.Equal("application/json", doc.DefaultContentType);
        Assert.False(string.IsNullOrEmpty(doc.Id));
    }
}

