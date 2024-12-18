using System;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Core.BenzeneMessage;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandling;
using Benzene.Core.Request;
using Benzene.Core.Serialization;
using Benzene.Test.Examples;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class RequestFactoryTest
{
    [Fact]
    public void GetsRequest()
    {
        var mockMessageMapper = new Mock<IRequestMapper<BenzeneMessageContext>>();
        mockMessageMapper.Setup(x => x.GetBody<ExampleRequestPayload>(It.IsAny<BenzeneMessageContext>()))
            .Returns(new ExampleRequestPayload());

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        var requestFactory = new RequestFactory<BenzeneMessageContext>(mockMessageMapper.Object, context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.NotNull(request);
    }

    [Fact]
    public void GetsRequest_No_Mappers_Returns_Null()
    {
        var serviceResolver = ServiceResolverMother.CreateServiceResolver();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        var requestFactory = new RequestFactory<BenzeneMessageContext>(
            new MultiSerializerOptionsRequestMapper<BenzeneMessageContext, JsonSerializer>(serviceResolver,
                Mock.Of<IMessageMapper<BenzeneMessageContext>>(),
                Array.Empty<ISerializerOption<BenzeneMessageContext>>(),
                Array.Empty<IRequestEnricher<BenzeneMessageContext>>()), context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.Null(request);
    }


    [Fact]
    public void GetsRequest_Default_Mapper_Returns_Request()
    {
        var mockMessageMapper = new Mock<IRequestMapper<BenzeneMessageContext>>();
        mockMessageMapper.Setup(x => x.GetBody<ExampleRequestPayload>(It.IsAny<BenzeneMessageContext>()))
            .Returns(new ExampleRequestPayload());

        var serviceResolver = ServiceResolverMother.CreateServiceResolver();

        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        var requestFactory = new RequestFactory<BenzeneMessageContext>(
            new MultiSerializerOptionsRequestMapper<BenzeneMessageContext, JsonSerializer>(serviceResolver,
                Mock.Of<IMessageMapper<BenzeneMessageContext>>(),
                new ISerializerOption<BenzeneMessageContext>[] { new SerializerOption<BenzeneMessageContext, JsonSerializer>(x => true) },
                Array.Empty<IRequestEnricher<BenzeneMessageContext>>()), context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.Null(request);
    }
}
