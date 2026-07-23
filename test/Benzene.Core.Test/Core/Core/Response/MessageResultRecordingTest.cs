using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.BenzeneMessage;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.Messages;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Core.Core.Response;

/// <summary>
/// The request/response result setters write their outcome by producing a response; they historically
/// left <see cref="IHasMessageResult.MessageResult"/> unset, which is what made
/// <c>benzene.messages.processed</c> tag such messages <c>result=&lt;missing&gt;</c>. These tests pin the
/// fix: a response setter now also records the outcome onto a context that can carry one.
/// </summary>
public class MessageResultRecordingTest
{
    private class FakeHttpContext : IHasMessageResult
    {
        public IBenzeneResult MessageResult { get; set; } = null!;
    }

    /// <summary>A no-op response container - these tests care about the recorded result, not the written response.</summary>
    private class NoopResponseHandlerContainer<TContext> : IResponseHandlerContainer<TContext>
    {
        public Task HandleAsync(TContext context, IMessageHandlerResult messageHandlerResult) => Task.CompletedTask;
    }

    [Fact]
    public async Task ResponseSetter_SuccessfulResult_RecordsSuccessfulMessageResult()
    {
        var context = new FakeHttpContext();
        var setter = new ResponseMessageHandlerResultSetterBase<FakeHttpContext>(new NoopResponseHandlerContainer<FakeHttpContext>());

        await setter.SetResultAsync(context, new MessageHandlerResult(BenzeneResult.Ok()));

        Assert.NotNull(context.MessageResult);
        Assert.True(context.MessageResult.IsSuccessful);
    }

    [Fact]
    public async Task ResponseSetter_FailedResult_RecordsUnsuccessfulMessageResult()
    {
        var context = new FakeHttpContext();
        var setter = new ResponseMessageHandlerResultSetterBase<FakeHttpContext>(new NoopResponseHandlerContainer<FakeHttpContext>());

        await setter.SetResultAsync(context, new MessageHandlerResult(BenzeneResult.NotFound("nope")));

        Assert.NotNull(context.MessageResult);
        Assert.False(context.MessageResult.IsSuccessful);
    }

    [Fact]
    public async Task ResponseSetter_DoesNotOverwriteAnAlreadyRecordedResult()
    {
        var already = BenzeneResult.Ok();
        var context = new FakeHttpContext { MessageResult = already };
        var setter = new ResponseMessageHandlerResultSetterBase<FakeHttpContext>(new NoopResponseHandlerContainer<FakeHttpContext>());

        await setter.SetResultAsync(context, new MessageHandlerResult(BenzeneResult.NotFound("nope")));

        Assert.Same(already, context.MessageResult);
    }

    [Fact]
    public async Task ResponseIfHandledSetter_WithRealTopic_RecordsMessageResult()
    {
        var context = new FakeHttpContext();
        var setter = new ResponseIfHandledMessageHandlerResultSetter<FakeHttpContext>(new NoopResponseHandlerContainer<FakeHttpContext>());

        await setter.SetResultAsync(context, new MessageHandlerResult(new Topic("real-topic"), null, BenzeneResult.Ok()));

        Assert.NotNull(context.MessageResult);
        Assert.True(context.MessageResult.IsSuccessful);
    }

    [Fact]
    public async Task ResponseIfHandledSetter_WithMissingTopic_RecordsNothing()
    {
        var context = new FakeHttpContext();
        var setter = new ResponseIfHandledMessageHandlerResultSetter<FakeHttpContext>(new NoopResponseHandlerContainer<FakeHttpContext>());

        // Unroutable/passthrough traffic isn't handled here, so no outcome is written for it.
        await setter.SetResultAsync(context, new MessageHandlerResult(Benzene.Core.MessageHandlers.Constants.Missing, null, BenzeneResult.Ok()));

        Assert.Null(context.MessageResult);
    }

    [Fact]
    public async Task BenzeneMessageSetter_RecordsMessageResultEvenWhenTheResponseIsSuppressed()
    {
        var context = new BenzeneMessageContext(new BenzeneMessageRequest());
        var setter = new BenzeneMessageHandlerResultSetter(
            new NoopResponseHandlerContainer<BenzeneMessageContext>(),
            new BenzeneMessageResponseSuppression { IsSuppressed = true });

        await setter.SetResultAsync(context, new MessageHandlerResult(BenzeneResult.Ok()));

        Assert.NotNull(context.MessageResult);
        Assert.True(context.MessageResult.IsSuccessful);
    }
}
