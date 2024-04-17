using System;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.Request;
using Benzene.Core.DirectMessage;
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
        var mockMessageMapper = new Mock<IRequestMapper<DirectMessageContext>>();
        mockMessageMapper.Setup(x => x.GetBody<ExampleRequestPayload>(It.IsAny<DirectMessageContext>()))
            .Returns(new ExampleRequestPayload());

        var context = DirectMessageContext.CreateInstance(new DirectMessageRequest());
        var requestFactory = new RequestFactory<DirectMessageContext>(mockMessageMapper.Object, context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.NotNull(request);
    }

    [Fact]
    public void GetsRequest_No_Mappers_Returns_Null()
    {
        var serviceResolver = ServiceResolverMother.CreateServiceResolver();

        var context = DirectMessageContext.CreateInstance(new DirectMessageRequest());
        var requestFactory = new RequestFactory<DirectMessageContext>(
            new MultiSerializerOptionsRequestMapper<DirectMessageContext, JsonSerializer>(serviceResolver,
                Mock.Of<IMessageMapper<DirectMessageContext>>(),
                Array.Empty<ISerializerOption<DirectMessageContext>>(),
                Array.Empty<IRequestEnricher<DirectMessageContext>>()), context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.Null(request);
    }


    [Fact]
    public void GetsRequest_Default_Mapper_Returns_Request()
    {
        var mockMessageMapper = new Mock<IRequestMapper<DirectMessageContext>>();
        mockMessageMapper.Setup(x => x.GetBody<ExampleRequestPayload>(It.IsAny<DirectMessageContext>()))
            .Returns(new ExampleRequestPayload());

        var serviceResolver = ServiceResolverMother.CreateServiceResolver();

        var context = DirectMessageContext.CreateInstance(new DirectMessageRequest());
        var requestFactory = new RequestFactory<DirectMessageContext>(
            new MultiSerializerOptionsRequestMapper<DirectMessageContext, JsonSerializer>(serviceResolver,
                Mock.Of<IMessageMapper<DirectMessageContext>>(),
                new ISerializerOption<DirectMessageContext>[] { new SerializerOption<DirectMessageContext, JsonSerializer>(x => true) },
                Array.Empty<IRequestEnricher<DirectMessageContext>>()), context);

        var request = requestFactory.GetRequest<ExampleRequestPayload>();

        Assert.Null(request);
    }
}
