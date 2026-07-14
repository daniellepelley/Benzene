using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Response;
using Xunit;

namespace Benzene.Test.Plugins.Xml;

public class XmlSerializationResponseHandlerTest
{
    private class TestContext
    {
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();
    }

    private class FakeHeadersGetter : IMessageHeadersGetter<TestContext>
    {
        public IDictionary<string, string> GetHeaders(TestContext context) => context.Headers;
    }

    private class FakeResponseAdapter : IBenzeneResponseAdapter<TestContext>
    {
        public string Body { get; private set; }
        public string ContentType { get; private set; }

        public void SetResponseHeader(TestContext context, string headerKey, string headerValue) { }
        public void SetContentType(TestContext context, string contentType) => ContentType = contentType;
        public void SetStatusCode(TestContext context, string statusCode) { }
        public void SetBody(TestContext context, string body) => Body = body;
        public string GetBody(TestContext context) => Body;
        public Task FinalizeAsync(TestContext context) => Task.CompletedTask;
    }

    private class RecordingBodySerializer : IBodySerializer
    {
        public ISerializer CapturedSerializer { get; private set; }

        public string Serialize(ISerializer serializer, IMessageHandlerResult messageHandlerResult)
        {
            CapturedSerializer = serializer;
            return "captured-xml";
        }
    }

    [Fact]
    public void HandleAsync_MatchingContentType_UsesTheDiRegisteredSerializer()
    {
        var context = new TestContext();
        context.Headers["content-type"] = "application/xml; charset=utf-8";

        var adapter = new FakeResponseAdapter();
        var injectedSerializer = new Benzene.Xml.XmlSerializer();
        var handler = new Benzene.Xml.XmlSerializationResponseHandler<TestContext>(adapter, new FakeHeadersGetter(), injectedSerializer);
        var bodySerializer = new RecordingBodySerializer();

        handler.HandleAsync(context, null, bodySerializer);

        Assert.Same(injectedSerializer, bodySerializer.CapturedSerializer);
        Assert.Equal("captured-xml", adapter.Body);
        Assert.Equal("application/xml", adapter.ContentType);
    }

    [Fact]
    public void HandleAsync_NonMatchingContentType_DoesNothing()
    {
        var context = new TestContext();
        context.Headers["content-type"] = "application/json";

        var adapter = new FakeResponseAdapter();
        var handler = new Benzene.Xml.XmlSerializationResponseHandler<TestContext>(adapter, new FakeHeadersGetter(), new Benzene.Xml.XmlSerializer());
        var bodySerializer = new RecordingBodySerializer();

        handler.HandleAsync(context, null, bodySerializer);

        Assert.Null(bodySerializer.CapturedSerializer);
        Assert.Null(adapter.Body);
    }

    [Fact]
    public void HandleAsync_NoHeaders_DoesNothing()
    {
        var adapter = new FakeResponseAdapter();
        var handler = new Benzene.Xml.XmlSerializationResponseHandler<TestContext>(adapter, new FakeHeadersGetter(), new Benzene.Xml.XmlSerializer());
        var bodySerializer = new RecordingBodySerializer();

        handler.HandleAsync(new TestContext(), null, bodySerializer);

        Assert.Null(bodySerializer.CapturedSerializer);
    }
}
