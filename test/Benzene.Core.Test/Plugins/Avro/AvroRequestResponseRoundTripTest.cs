using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Avro;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.Messages;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Microsoft.Dependencies;
using Benzene.Results;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Constants = Benzene.Core.MessageHandlers.Constants;

namespace Benzene.Test.Plugins.Avro;

/// <summary>
/// End-to-end proof that Avro works through the real request-mapping (Phase 2 negotiation + Phase 4
/// byte path) and response-rendering (Phase 3 renderer seam) pipeline for <c>BenzeneMessageContext</c>,
/// using a real DI-wired container rather than hand-constructed fakes. Mirrors the MessagePack sibling.
/// </summary>
public class AvroRequestResponseRoundTripTest
{
    private static MicrosoftServiceResolverAdapter CreateServiceResolver()
    {
        var services = ServiceResolverMother.CreateServiceCollection();
        services.UsingBenzene(x => x.AddBenzeneMessage().AddAvro());
        return new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
    }

    [Fact]
    public void Request_NegotiatesAvroAndUsesTheBytePath()
    {
        var serializer = new AvroSerializer();
        var body = serializer.Serialize(new ExampleRequestPayload { Name = "some-name" });

        var context = new BenzeneMessageContext(new BenzeneMessageRequest
        {
            Headers = new Dictionary<string, string> { { "content-type", "application/avro" } },
            Body = body
        });

        var serviceResolver = CreateServiceResolver();

        // BenzeneMessageContext has IMessageBodyBytesGetter registered and AvroSerializer implements
        // IPayloadSerializer, so the mapper takes the byte-oriented path, not the string one.
        var mediaFormatNegotiator = serviceResolver.GetService<IMediaFormatNegotiator<BenzeneMessageContext>>();
        var requestMapper = new MultiSerializerOptionsRequestMapper<BenzeneMessageContext>(
            mediaFormatNegotiator, serviceResolver, new BenzeneMessageGetter(),
            Array.Empty<IRequestEnricher<BenzeneMessageContext>>());

        var request = requestMapper.GetBody<ExampleRequestPayload>(context);

        Assert.Equal("some-name", request!.Name);
    }

    [Fact]
    public async Task Response_NegotiatesAvroAndRendersTheResult()
    {
        var serviceResolver = CreateServiceResolver();
        var serializer = new AvroSerializer();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest
        {
            Headers = new Dictionary<string, string> { { "accept", "application/avro" } }
        });

        var mediaFormatNegotiator = serviceResolver.GetService<IMediaFormatNegotiator<BenzeneMessageContext>>();
        var responsePayloadMapper = serviceResolver.GetService<IResponsePayloadMapper<BenzeneMessageContext>>();
        var responseAdapter = serviceResolver.GetService<IBenzeneResponseAdapter<BenzeneMessageContext>>();

        var renderer = new SerializerResponseRenderer<BenzeneMessageContext>(
            responsePayloadMapper, mediaFormatNegotiator, serviceResolver);

        var handlerDefinition = MessageHandlerDefinition.CreateInstance(Defaults.Topic, Defaults.Version2,
            typeof(ExampleRequestPayload), typeof(ExampleResponsePayload), typeof(ExampleMessageHandler));
        var result = new MessageHandlerResult(new Topic(Defaults.Topic), handlerDefinition,
            BenzeneResult.Ok(Mother.CreateResponse("resp-name")));

        await renderer.RenderAsync(context, result, responseAdapter);

        Assert.Equal("application/avro", context.BenzeneMessageResponse.Headers[Constants.ContentTypeHeader]);
        var decoded = serializer.Deserialize<ExampleResponsePayload>(context.BenzeneMessageResponse.Body);
        Assert.Equal("resp-name", decoded!.Name);
    }
}
