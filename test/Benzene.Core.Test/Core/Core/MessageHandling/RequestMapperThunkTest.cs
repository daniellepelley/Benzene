using System;
using System.Collections.Generic;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.DI;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Request;
using Benzene.Core.Serialization;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Core.Core.Mappers;
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
        
        var requestMapper = new RequestMapper<BenzeneMessageContext>(new BenzeneMessageMapper(), serializer);
        var requestFactory = new RequestMapperThunk<BenzeneMessageContext>(requestMapper, context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.NotNull(request);
    }

    [Fact]
    public void GetsRequest_No_Mappers_Returns_Null()
    {
        var serviceResolver = ServiceResolverMother.CreateServiceResolver();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        var requestFactory = new RequestMapperThunk<BenzeneMessageContext>(
            new MultiSerializerOptionsRequestMapper<BenzeneMessageContext, JsonSerializer>(serviceResolver,
                Mock.Of<IMessageMapper<BenzeneMessageContext>>(),
                Array.Empty<ISerializerOption<BenzeneMessageContext>>(),
                Array.Empty<IRequestEnricher<BenzeneMessageContext>>()), context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.NotNull(request);
    }


    [Fact]
    public void GetsRequest_Default_Mapper_Returns_Request()
    {
        var mockMessageMapper = new Mock<IRequestMapper<BenzeneMessageContext>>();
        mockMessageMapper.Setup(x => x.GetBody<ExampleRequestPayload>(It.IsAny<BenzeneMessageContext>()))
            .Returns(new ExampleRequestPayload());

        var serviceResolver = ServiceResolverMother.CreateServiceResolver();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        var requestFactory = new RequestMapperThunk<BenzeneMessageContext>(
            new MultiSerializerOptionsRequestMapper<BenzeneMessageContext, JsonSerializer>(serviceResolver,
                Mock.Of<IMessageMapper<BenzeneMessageContext>>(),
                new ISerializerOption<BenzeneMessageContext>[] { new SerializerOption<BenzeneMessageContext, JsonSerializer>(x => x.Always()) },
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
        var requestMapper = new MultiSerializerOptionsRequestMapper<BenzeneMessageContext, JsonSerializer>(serviceResolver,
                new BenzeneMessageMapper(),
                new ISerializerOption<BenzeneMessageContext>[] { new SerializerOption<BenzeneMessageContext, JsonSerializer>(x => x.Always()) },
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
        
        var requestMapper = new MultiSerializerOptionsRequestMapper<BenzeneMessageContext, JsonSerializer>(
                new MicrosoftServiceResolverAdapter(services.BuildServiceProvider()),
                new BenzeneMessageMapper(),
                new ISerializerOption<BenzeneMessageContext>[]
                {
                    new SerializerOption<BenzeneMessageContext, XmlSerializer>(x => x.CheckHeader("content-type", "application/xml"))
                },
                Array.Empty<IRequestEnricher<BenzeneMessageContext>>());
       
        var requestFactory = new RequestMapperThunk<BenzeneMessageContext>(requestMapper, context);
        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.Equal("some-name", request!.Name);
    }
}
