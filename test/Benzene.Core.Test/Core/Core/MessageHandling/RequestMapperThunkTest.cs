using System;
using System.Collections.Generic;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Extras.Request;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Benzene.Xml;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class RequestMapperThunkTest
{
    [Fact]
    public void GetsRequest()
    {
        var serializer = new JsonSerializer();
        var context = new BenzeneMessageContext(new BenzeneMessageRequest
        {
            Body = serializer.Serialize(new ExampleRequestPayload { Name = "some-name"})
        });

        var requestMapper = new RequestMapper<BenzeneMessageContext>(new BenzeneMessageGetter(), serializer);
        var requestFactory = new RequestMapperThunk<BenzeneMessageContext>(requestMapper, context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.NotNull(request);
    }

    [Fact]
    public void GetsRequest_No_Mappers_Returns_Null()
    {
        var serviceResolver = ServiceResolverMother.CreateServiceResolver();
        var mediaFormatNegotiator = new MediaFormatNegotiator<BenzeneMessageContext>(
            Array.Empty<IMediaFormat<BenzeneMessageContext>>(),
            new JsonMediaFormat<BenzeneMessageContext>(new JsonSerializer()),
            serviceResolver);

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        var requestFactory = new RequestMapperThunk<BenzeneMessageContext>(
            new MultiSerializerOptionsRequestMapper<BenzeneMessageContext>(mediaFormatNegotiator,
                serviceResolver,
                Mock.Of<IMessageBodyGetter<BenzeneMessageContext>>(),
                Array.Empty<IRequestEnricher<BenzeneMessageContext>>()), context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.NotNull(request);
    }


    [Fact]
    public void GetsRequest_Default_Mapper_Returns_Request()
    {
        var serviceResolver = ServiceResolverMother.CreateServiceResolver();
        var mediaFormatNegotiator = new MediaFormatNegotiator<BenzeneMessageContext>(
            new IMediaFormat<BenzeneMessageContext>[] { new InlineMediaFormat<BenzeneMessageContext>("application/json", new JsonSerializer(), _ => true) },
            new JsonMediaFormat<BenzeneMessageContext>(new JsonSerializer()),
            serviceResolver);

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        var requestFactory = new RequestMapperThunk<BenzeneMessageContext>(
            new MultiSerializerOptionsRequestMapper<BenzeneMessageContext>(mediaFormatNegotiator,
                serviceResolver,
                Mock.Of<IMessageBodyGetter<BenzeneMessageContext>>(),
                Array.Empty<IRequestEnricher<BenzeneMessageContext>>()), context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.Null(request!.Name);
    }


    [Fact]
    public void GetsRequest_Multi()
    {
        var serializer = new JsonSerializer();
        var context = new BenzeneMessageContext(new BenzeneMessageRequest
        {
            Body = serializer.Serialize(new ExampleRequestPayload { Name = "some-name"})
        });

        var serviceResolver = ServiceResolverMother.CreateServiceResolver();
        var mediaFormatNegotiator = new MediaFormatNegotiator<BenzeneMessageContext>(
            new IMediaFormat<BenzeneMessageContext>[] { new InlineMediaFormat<BenzeneMessageContext>("application/json", serializer, _ => true) },
            new JsonMediaFormat<BenzeneMessageContext>(serializer),
            serviceResolver);

        var requestMapper = new MultiSerializerOptionsRequestMapper<BenzeneMessageContext>(mediaFormatNegotiator,
                serviceResolver,
                new BenzeneMessageGetter(),
                Array.Empty<IRequestEnricher<BenzeneMessageContext>>());

        var requestFactory = new RequestMapperThunk<BenzeneMessageContext>(requestMapper, context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.NotNull(request);
    }

    [Fact]
    public void GetsRequest_Multi_Xml()
    {
        var serializer = new XmlSerializer();
        var context = new BenzeneMessageContext(new BenzeneMessageRequest
        {
            Headers = new Dictionary<string, string> { { "content-type", "application/xml" }},
            Body = serializer.Serialize(new ExampleRequestPayload { Name = "some-name"})
        });

        var services = ServiceResolverMother.CreateServiceCollection();
        services.UsingBenzene(x => x.AddBenzeneMessage().AddXml());

        var serviceResolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());
        var mediaFormatNegotiator = serviceResolver.GetService<IMediaFormatNegotiator<BenzeneMessageContext>>();

        var requestMapper = new MultiSerializerOptionsRequestMapper<BenzeneMessageContext>(
                mediaFormatNegotiator,
                serviceResolver,
                new BenzeneMessageGetter(),
                Array.Empty<IRequestEnricher<BenzeneMessageContext>>());

        var requestFactory = new RequestMapperThunk<BenzeneMessageContext>(requestMapper, context);
        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.Equal("some-name", request!.Name);
    }

    [Fact]
    public void GetsRequest_BenzeneMessageContext_IsWiredForTheBytePath()
    {
        var serializer = new JsonSerializer();
        var body = serializer.Serialize(new ExampleRequestPayload { Name = "some-name" });
        var context = new BenzeneMessageContext(new BenzeneMessageRequest { Body = body });

        var services = ServiceResolverMother.CreateServiceCollection();
        services.UsingBenzene(x => x.AddBenzeneMessage());

        var serviceResolver = new MicrosoftServiceResolverAdapter(services.BuildServiceProvider());

        // Phase 4's reference transport: BenzeneMessageContext registers IMessageBodyBytesGetter,
        // and JsonSerializer implements IPayloadSerializer, so MultiSerializerOptionsRequestMapper
        // resolves both and prefers the byte path.
        var bytesGetter = serviceResolver.GetService<IMessageBodyBytesGetter<BenzeneMessageContext>>();
        Assert.Equal(body, System.Text.Encoding.UTF8.GetString(bytesGetter.GetBodyBytes(context).Span));

        var mediaFormatNegotiator = serviceResolver.GetService<IMediaFormatNegotiator<BenzeneMessageContext>>();
        var requestMapper = new MultiSerializerOptionsRequestMapper<BenzeneMessageContext>(
            mediaFormatNegotiator, serviceResolver, new BenzeneMessageGetter(),
            Array.Empty<IRequestEnricher<BenzeneMessageContext>>());

        var requestFactory = new RequestMapperThunk<BenzeneMessageContext>(requestMapper, context);
        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.Equal("some-name", request!.Name);
    }
}
