using System;
using System.Threading.Tasks;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class DeriveTopicMiddlewareTest
{
    // A transport context whose payload carries the routing discriminator but no topic attribute.
    private class TestContext
    {
        public string EventType { get; init; } = "";
    }

    private class FixedMessageTopicGetter : IMessageTopicGetter<TestContext>
    {
        private readonly ITopic? _topic;
        public FixedMessageTopicGetter(ITopic? topic) => _topic = topic;
        public ITopic? GetTopic(TestContext context) => _topic;
    }

    [Fact]
    public async Task HandleAsync_DerivesTopicFromContext_SetsHolder_ThenCallsNext()
    {
        var holder = new PresetTopicHolder();
        var nextCalled = false;
        var middleware = new DeriveTopicMiddleware<TestContext>(holder, ctx => new Topic($"orders.{ctx.EventType}"));

        await middleware.HandleAsync(new TestContext { EventType = "created" }, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.Equal("orders.created", holder.PresetTopic?.Id);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task HandleAsync_SelectorReturnsNull_LeavesHolderUnset_AndStillCallsNext()
    {
        var holder = new PresetTopicHolder();
        var nextCalled = false;
        var middleware = new DeriveTopicMiddleware<TestContext>(holder, _ => null);

        await middleware.HandleAsync(new TestContext(), () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.Null(holder.PresetTopic);
        Assert.True(nextCalled);
    }

    // The whole point: a derived topic flows through PresetTopicMessageTopicGetter exactly like a
    // preset one, in preference to a transport getter that couldn't find a topic on the payload.
    [Fact]
    public async Task DerivedTopic_IsResolvedByPresetTopicMessageTopicGetter_OverANullTransportGetter()
    {
        var holder = new PresetTopicHolder();
        var getter = new PresetTopicMessageTopicGetter<TestContext>(new FixedMessageTopicGetter(topic: null), holder);
        var context = new TestContext { EventType = "shipped" };

        // Before the middleware runs, the transport getter has nothing to route on.
        Assert.Null(getter.GetTopic(context));

        var middleware = new DeriveTopicMiddleware<TestContext>(holder, ctx => new Topic($"orders.{ctx.EventType}", "v2"));
        await middleware.HandleAsync(context, () => Task.CompletedTask);

        var resolved = getter.GetTopic(context);
        Assert.Equal("orders.shipped", resolved?.Id);
        Assert.Equal("v2", resolved?.Version);
    }
}
