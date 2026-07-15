using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.MessagePack;
using Moq;
using Xunit;

namespace Benzene.Test.Plugins.MessagePack;

public class MessagePackMediaFormatTest
{
    private class TestContext
    {
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();
    }

    private class FakeHeadersGetter : IMessageHeadersGetter<TestContext>
    {
        public IDictionary<string, string> GetHeaders(TestContext context) => context.Headers;
    }

    private static IServiceResolver CreateServiceResolver()
    {
        var serviceResolver = new Mock<IServiceResolver>();
        serviceResolver.Setup(x => x.GetService<IMessageHeadersGetter<TestContext>>()).Returns(new FakeHeadersGetter());
        return serviceResolver.Object;
    }

    [Fact]
    public void ContentType_IsApplicationMsgpack()
    {
        var format = new MessagePackMediaFormat<TestContext>(new MessagePackSerializer());

        Assert.Equal("application/msgpack", format.ContentType);
    }

    [Fact]
    public void GetSerializer_ReturnsTheInjectedSerializer()
    {
        var injectedSerializer = new MessagePackSerializer();
        var format = new MessagePackMediaFormat<TestContext>(injectedSerializer);

        Assert.Same(injectedSerializer, format.GetSerializer(Mock.Of<IServiceResolver>()));
    }

    [Fact]
    public void CanRead_MatchingContentType_ReturnsTrue()
    {
        var context = new TestContext();
        context.Headers["content-type"] = "application/msgpack; charset=utf-8";

        var format = new MessagePackMediaFormat<TestContext>(new MessagePackSerializer());

        Assert.True(format.CanRead(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanRead_NonMatchingContentType_ReturnsFalse()
    {
        var context = new TestContext();
        context.Headers["content-type"] = "application/json";

        var format = new MessagePackMediaFormat<TestContext>(new MessagePackSerializer());

        Assert.False(format.CanRead(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanRead_NoHeaders_ReturnsFalse()
    {
        var format = new MessagePackMediaFormat<TestContext>(new MessagePackSerializer());

        Assert.False(format.CanRead(new TestContext(), CreateServiceResolver()));
    }

    [Fact]
    public void CanWrite_AcceptHeaderContainsMsgpack_ReturnsTrue()
    {
        var context = new TestContext();
        context.Headers["accept"] = "application/json, application/msgpack;q=0.9";

        var format = new MessagePackMediaFormat<TestContext>(new MessagePackSerializer());

        Assert.True(format.CanWrite(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanWrite_AcceptHeaderMissingMsgpack_ReturnsFalse()
    {
        var context = new TestContext();
        context.Headers["accept"] = "application/json";

        var format = new MessagePackMediaFormat<TestContext>(new MessagePackSerializer());

        Assert.False(format.CanWrite(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanWrite_NoAcceptHeader_ReturnsFalse()
    {
        var format = new MessagePackMediaFormat<TestContext>(new MessagePackSerializer());

        Assert.False(format.CanWrite(new TestContext(), CreateServiceResolver()));
    }
}
