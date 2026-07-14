using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages;
using Benzene.Extras.Request;
using Benzene.Results;
using Benzene.Test.Examples;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.Response;

public class SerializationResponseHandlerTest
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

    [Fact]
    public async Task HandleAsync_UsesTheNegotiatedFormat()
    {
        var adapter = new FakeResponseAdapter();
        var serializer = new JsonSerializer();
        var mediaFormat = new InlineMediaFormat<TestContext>("application/test-format", serializer, _ => true);
        var mediaFormatNegotiator = new MediaFormatNegotiator<TestContext>(
            new IMediaFormat<TestContext>[] { mediaFormat },
            new JsonMediaFormat<TestContext>(serializer),
            Mock.Of<IServiceResolver>());

        var handler = new SerializationResponseHandler<TestContext>(adapter,
            new DefaultResponsePayloadMapper<TestContext>(), mediaFormatNegotiator, Mock.Of<IServiceResolver>());

        var messageHandlerDefinition = Mother.CreateMessageHandlerDefinitionV2();
        var request = Mother.CreateRequest();
        var result = new MessageHandlerResult(new Topic(Defaults.Topic), messageHandlerDefinition, BenzeneResult.Ok(request));

        await handler.HandleAsync(new TestContext(), result);

        Assert.Equal(serializer.Serialize(messageHandlerDefinition.ResponseType, request), adapter.Body);
        Assert.Equal("application/test-format", adapter.ContentType);
    }

    [Fact]
    public async Task HandleAsync_BodyAlreadySet_DoesNothing()
    {
        var adapter = new FakeResponseAdapter();
        adapter.SetBody(new TestContext(), "already-set");
        var serializer = new JsonSerializer();
        var mediaFormatNegotiator = new MediaFormatNegotiator<TestContext>(
            Array.Empty<IMediaFormat<TestContext>>(),
            new JsonMediaFormat<TestContext>(serializer),
            Mock.Of<IServiceResolver>());

        var handler = new SerializationResponseHandler<TestContext>(adapter,
            new DefaultResponsePayloadMapper<TestContext>(), mediaFormatNegotiator, Mock.Of<IServiceResolver>());

        var messageHandlerDefinition = Mother.CreateMessageHandlerDefinitionV2();
        var result = new MessageHandlerResult(new Topic(Defaults.Topic), messageHandlerDefinition, BenzeneResult.Ok(Mother.CreateRequest()));

        await handler.HandleAsync(new TestContext(), result);

        Assert.Equal("already-set", adapter.Body);
        Assert.Null(adapter.ContentType);
    }
}
