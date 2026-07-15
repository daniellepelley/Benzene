using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Xml;
using Moq;
using Xunit;

namespace Benzene.Test.Plugins.Xml;

public class XmlMediaFormatTest
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
    public void ContentType_IsApplicationXml()
    {
        var format = new XmlMediaFormat<TestContext>(new XmlSerializer());

        Assert.Equal("application/xml", format.ContentType);
    }

    [Fact]
    public void GetSerializer_ReturnsTheInjectedSerializer()
    {
        var injectedSerializer = new XmlSerializer();
        var format = new XmlMediaFormat<TestContext>(injectedSerializer);

        Assert.Same(injectedSerializer, format.GetSerializer(Mock.Of<IServiceResolver>()));
    }

    [Fact]
    public void CanRead_MatchingContentType_ReturnsTrue()
    {
        var context = new TestContext();
        context.Headers["content-type"] = "application/xml; charset=utf-8";

        var format = new XmlMediaFormat<TestContext>(new XmlSerializer());

        Assert.True(format.CanRead(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanRead_NonMatchingContentType_ReturnsFalse()
    {
        var context = new TestContext();
        context.Headers["content-type"] = "application/json";

        var format = new XmlMediaFormat<TestContext>(new XmlSerializer());

        Assert.False(format.CanRead(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanRead_NoHeaders_ReturnsFalse()
    {
        var format = new XmlMediaFormat<TestContext>(new XmlSerializer());

        Assert.False(format.CanRead(new TestContext(), CreateServiceResolver()));
    }

    [Fact]
    public void CanWrite_AcceptHeaderContainsXml_ReturnsTrue()
    {
        var context = new TestContext();
        context.Headers["accept"] = "application/json, application/xml;q=0.9";

        var format = new XmlMediaFormat<TestContext>(new XmlSerializer());

        Assert.True(format.CanWrite(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanWrite_AcceptHeaderMissingXml_ReturnsFalse()
    {
        var context = new TestContext();
        context.Headers["accept"] = "application/json";

        var format = new XmlMediaFormat<TestContext>(new XmlSerializer());

        Assert.False(format.CanWrite(context, CreateServiceResolver()));
    }

    [Fact]
    public void CanWrite_NoAcceptHeader_ReturnsFalse()
    {
        var format = new XmlMediaFormat<TestContext>(new XmlSerializer());

        Assert.False(format.CanWrite(new TestContext(), CreateServiceResolver()));
    }
}
