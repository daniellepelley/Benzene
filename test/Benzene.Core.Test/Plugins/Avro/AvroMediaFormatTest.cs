using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Avro;
using Moq;
using Xunit;

namespace Benzene.Test.Plugins.Avro;

public class AvroMediaFormatTest
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
    public void ContentType_IsApplicationAvro()
    {
        var format = new AvroMediaFormat<TestContext>(new AvroSerializer());

        Assert.Equal("application/avro", format.ContentType);
    }

    [Fact]
    public void GetSerializer_ReturnsTheInjectedSerializer()
    {
        var injectedSerializer = new AvroSerializer();
        var format = new AvroMediaFormat<TestContext>(injectedSerializer);

        Assert.Same(injectedSerializer, format.GetSerializer(Mock.Of<IServiceResolver>()));
    }

    [Fact]
    public void CanRead_MatchingContentType_ReturnsTrue()
    {
        var context = new TestContext();
        context.Headers["content-type"] = "application/avro";

        var format = new AvroMediaFormat<TestContext>(new AvroSerializer());

        Assert.True(format.CanRead(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanRead_NonMatchingContentType_ReturnsFalse()
    {
        var context = new TestContext();
        context.Headers["content-type"] = "application/json";

        var format = new AvroMediaFormat<TestContext>(new AvroSerializer());

        Assert.False(format.CanRead(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanWrite_AcceptHeaderContainsAvro_ReturnsTrue()
    {
        var context = new TestContext();
        context.Headers["accept"] = "application/json, application/avro;q=0.9";

        var format = new AvroMediaFormat<TestContext>(new AvroSerializer());

        Assert.True(format.CanWrite(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanWrite_AcceptHeaderMissingAvro_ReturnsFalse()
    {
        var context = new TestContext();
        context.Headers["accept"] = "application/json";

        var format = new AvroMediaFormat<TestContext>(new AvroSerializer());

        Assert.False(format.CanWrite(context, CreateServiceResolver()));
    }
}
