using System.Collections.Generic;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Test.Examples;
using Benzene.Xml;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MediaFormats;

public class MediaFormatNegotiatorTest
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

    private static MediaFormatNegotiator<TestContext> CreateNegotiator(params IMediaFormat<TestContext>[] formats)
    {
        var resolver = CreateServiceResolver();
        return new MediaFormatNegotiator<TestContext>(formats, new JsonMediaFormat<TestContext>(new JsonSerializer()), resolver);
    }

    public static IEnumerable<object[]> NegotiationCases()
    {
        // content-type header, accept header, expected read content type, expected write content type
        yield return new object[] { "application/xml", null, "application/xml", "application/xml" };
        yield return new object[] { null, "application/xml", "application/json", "application/xml" };
        yield return new object[] { "application/json", "application/xml", "application/json", "application/xml" };
        yield return new object[] { null, null, "application/json", "application/json" };
        // No format's accept matches ("application/json" isn't xml) - write falls back to whatever
        // was selected for read (xml), not to the process default (json), since only one candidate
        // format is registered here and the default is never itself a negotiation candidate.
        yield return new object[] { "application/xml", "application/json", "application/xml", "application/xml" };
        // q-values are not parsed - any comma-delimited token that matches the format's content type
        // is a hit, regardless of its position or quality parameter.
        yield return new object[] { "application/xml", "application/json, application/xml;q=0.9", "application/xml", "application/xml" };
    }

    [Theory]
    [MemberData(nameof(NegotiationCases))]
    public void SelectsFormats_ForEveryContentTypeAcceptCombination(
        string contentType, string accept, string expectedReadContentType, string expectedWriteContentType)
    {
        var context = new TestContext();
        if (contentType != null) context.Headers["content-type"] = contentType;
        if (accept != null) context.Headers["accept"] = accept;

        var xmlFormat = new XmlMediaFormat<TestContext>(new XmlSerializer());
        var negotiator = CreateNegotiator(xmlFormat);

        Assert.Equal(expectedReadContentType, negotiator.SelectRead(context).ContentType);
    }

    [Theory]
    [MemberData(nameof(NegotiationCases))]
    public void SelectsWriteFormats_ForEveryContentTypeAcceptCombination(
        string contentType, string accept, string expectedReadContentType, string expectedWriteContentType)
    {
        var context = new TestContext();
        if (contentType != null) context.Headers["content-type"] = contentType;
        if (accept != null) context.Headers["accept"] = accept;

        var xmlFormat = new XmlMediaFormat<TestContext>(new XmlSerializer());
        var negotiator = CreateNegotiator(xmlFormat);

        Assert.Equal(expectedWriteContentType, negotiator.SelectWrite(context).ContentType);
    }

    private static InlineMediaFormat<TestContext> CreateJsonFormat() =>
        new InlineMediaFormat<TestContext>("application/json", new JsonSerializer(),
            (ctx, _) => ctx.Headers.TryGetValue("content-type", out var ct) && ct == "application/json",
            (ctx, _) => ctx.Headers.TryGetValue("accept", out var accept) && accept.Contains("application/json"));

    [Theory]
    [InlineData("application/xml", "application/xml", "application/xml", "application/xml")]
    [InlineData("application/json", "application/json", "application/json", "application/json")]
    public void RegistrationOrder_JsonThenXmlVsXmlThenJson_ProducesIdenticalSelection(
        string contentType, string accept, string expectedReadContentType, string expectedWriteContentType)
    {
        var context = new TestContext();
        context.Headers["content-type"] = contentType;
        context.Headers["accept"] = accept;

        var jsonThenXml = CreateNegotiator(CreateJsonFormat(), new XmlMediaFormat<TestContext>(new XmlSerializer()));
        var xmlThenJson = CreateNegotiator(new XmlMediaFormat<TestContext>(new XmlSerializer()), CreateJsonFormat());

        Assert.Equal(expectedReadContentType, jsonThenXml.SelectRead(context).ContentType);
        Assert.Equal(expectedReadContentType, xmlThenJson.SelectRead(context).ContentType);
        Assert.Equal(expectedWriteContentType, jsonThenXml.SelectWrite(context).ContentType);
        Assert.Equal(expectedWriteContentType, xmlThenJson.SelectWrite(context).ContentType);
    }

    [Fact]
    public void SelectRead_IsMemoizedPerNegotiatorInstance()
    {
        var context = new TestContext();
        context.Headers["content-type"] = "application/xml";

        var negotiator = CreateNegotiator(new XmlMediaFormat<TestContext>(new XmlSerializer()));

        var first = negotiator.SelectRead(context);
        var second = negotiator.SelectRead(context);

        Assert.Same(first, second);
    }
}
