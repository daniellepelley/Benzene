using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages;
using Benzene.Results;
using Benzene.Test.Examples;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.Response;

/// <summary>
/// Proof, not product (Phase 3 of docs/plans/request-response-improvements-plan.md): a test-only
/// HTML renderer demonstrating that <see cref="IResponseRenderer{TContext}"/> supports a
/// representation beyond JSON/XML with zero changes to core - a real <c>Benzene.Html.*</c> package is
/// future work.
/// </summary>
public class FakeHtmlRendererTest
{
    private class TestContext
    {
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();
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

    private class FakeHtmlRenderer : IResponseRenderer<TestContext>
    {
        private readonly Task _gate;

        public FakeHtmlRenderer(Task gate = null)
        {
            _gate = gate ?? Task.CompletedTask;
        }

        public bool CanRender(TestContext context, IMessageHandlerResult result, IServiceResolver resolver) =>
            context.Headers.TryGetValue("accept", out var accept) && accept.Contains("text/html");

        // Owns its own error representation instead of DefaultResponsePayloadMapper's ErrorPayload JSON.
        public async Task RenderAsync(TestContext context, IMessageHandlerResult result, IBenzeneResponseAdapter<TestContext> response)
        {
            await _gate;

            response.SetBody(context, result.BenzeneResult.IsSuccessful
                ? "<html><body>ok</body></html>"
                : "<html><body>error</body></html>");
            response.SetContentType(context, "text/html");
        }
    }

    private class RawHtmlMessage : IRawContentMessage
    {
        public RawHtmlMessage(string content) => Content = content;
        public string Content { get; }
        public string ContentType => "text/html";
    }

    private static MediaFormatNegotiator<TestContext> CreateJsonOnlyNegotiator() =>
        new(Array.Empty<IMediaFormat<TestContext>>(), new JsonMediaFormat<TestContext>(new JsonSerializer()), Mock.Of<IServiceResolver>());

    [Fact]
    public async Task AcceptTextHtml_SelectsHtmlRendererOverJson()
    {
        var adapter = new FakeResponseAdapter();
        var renderers = new IResponseRenderer<TestContext>[]
        {
            new FakeHtmlRenderer(),
            new SerializerResponseRenderer<TestContext>(new DefaultResponsePayloadMapper<TestContext>(), CreateJsonOnlyNegotiator(), Mock.Of<IServiceResolver>())
        };
        var handler = new RendererResponseHandler<TestContext>(adapter, renderers, Mock.Of<IServiceResolver>());

        var context = new TestContext();
        context.Headers["accept"] = "text/html";

        var messageHandlerDefinition = Mother.CreateMessageHandlerDefinitionV2();
        var result = new MessageHandlerResult(new Topic(Defaults.Topic), messageHandlerDefinition, BenzeneResult.Ok(Mother.CreateRequest()));

        await handler.HandleAsync(context, result);

        Assert.Equal("text/html", adapter.ContentType);
        Assert.Equal("<html><body>ok</body></html>", adapter.Body);
    }

    [Fact]
    public async Task RenderAsync_IsGenuinelyAwaited_NotFireAndForget()
    {
        var gate = new TaskCompletionSource();
        var adapter = new FakeResponseAdapter();
        var renderers = new IResponseRenderer<TestContext>[] { new FakeHtmlRenderer(gate.Task) };
        var handler = new RendererResponseHandler<TestContext>(adapter, renderers, Mock.Of<IServiceResolver>());

        var context = new TestContext();
        context.Headers["accept"] = "text/html";

        var messageHandlerDefinition = Mother.CreateMessageHandlerDefinitionV2();
        var result = new MessageHandlerResult(new Topic(Defaults.Topic), messageHandlerDefinition, BenzeneResult.Ok(Mother.CreateRequest()));

        var handleTask = handler.HandleAsync(context, result).AsTask();

        Assert.False(handleTask.IsCompleted);
        Assert.Null(adapter.Body);

        gate.SetResult();
        await handleTask;

        Assert.Equal("<html><body>ok</body></html>", adapter.Body);
    }

    [Fact]
    public async Task FailedResult_RendersItsOwnErrorPage_NotErrorPayloadJson()
    {
        var adapter = new FakeResponseAdapter();
        var renderers = new IResponseRenderer<TestContext>[] { new FakeHtmlRenderer() };
        var handler = new RendererResponseHandler<TestContext>(adapter, renderers, Mock.Of<IServiceResolver>());

        var context = new TestContext();
        context.Headers["accept"] = "text/html";

        var messageHandlerDefinition = Mother.CreateMessageHandlerDefinitionV2();
        var result = new MessageHandlerResult(new Topic(Defaults.Topic), messageHandlerDefinition,
            BenzeneResult.NotFound<ExampleResponsePayload>("not found"));

        await handler.HandleAsync(context, result);

        Assert.Equal("<html><body>error</body></html>", adapter.Body);
        Assert.DoesNotContain("Detail", adapter.Body);
        Assert.DoesNotContain("not found", adapter.Body);
    }

    [Fact]
    public async Task RawContentMessage_DeliveredVerbatimWithItsOwnContentType()
    {
        var adapter = new FakeResponseAdapter();
        var renderers = new IResponseRenderer<TestContext>[]
        {
            new SerializerResponseRenderer<TestContext>(new DefaultResponsePayloadMapper<TestContext>(), CreateJsonOnlyNegotiator(), Mock.Of<IServiceResolver>())
        };
        var handler = new RendererResponseHandler<TestContext>(adapter, renderers, Mock.Of<IServiceResolver>());

        var context = new TestContext();

        var messageHandlerDefinition = MessageHandlerDefinition.CreateInstance(Defaults.Topic, Defaults.Version2,
            typeof(ExampleRequestPayload), typeof(RawHtmlMessage), typeof(ExampleMessageHandler));
        var result = new MessageHandlerResult(new Topic(Defaults.Topic), messageHandlerDefinition,
            BenzeneResult.Ok<object>(new RawHtmlMessage("<p>hi</p>")));

        await handler.HandleAsync(context, result);

        Assert.Equal("<p>hi</p>", adapter.Body);
        Assert.Equal("text/html", adapter.ContentType);
    }
}
