using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;
using Xunit;

namespace Benzene.Test.Core.Core.Response;

public class JsonSerializationResponseHandlerTest
{
    private class TestContext { }

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
            return "captured-body";
        }
    }

    [Fact]
    public void HandleAsync_UsesTheDiRegisteredSerializer_NotAFreshInstance()
    {
        var adapter = new FakeResponseAdapter();
        var injectedSerializer = new JsonSerializer();
        var handler = new JsonSerializationResponseHandler<TestContext>(adapter, injectedSerializer);
        var bodySerializer = new RecordingBodySerializer();

        handler.HandleAsync(new TestContext(), null, bodySerializer);

        Assert.Same(injectedSerializer, bodySerializer.CapturedSerializer);
        Assert.Equal("captured-body", adapter.Body);
        Assert.Equal("application/json", adapter.ContentType);
    }

    [Fact]
    public void HandleAsync_BodyAlreadySet_DoesNothing()
    {
        var adapter = new FakeResponseAdapter();
        adapter.SetBody(new TestContext(), "already-set");
        var handler = new JsonSerializationResponseHandler<TestContext>(adapter, new JsonSerializer());
        var bodySerializer = new RecordingBodySerializer();

        handler.HandleAsync(new TestContext(), null, bodySerializer);

        Assert.Null(bodySerializer.CapturedSerializer);
        Assert.Equal("already-set", adapter.Body);
    }
}
