using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Core.Messages;
using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class MessageGetterMemoizationTest
{
    private class TestContext
    {
    }

    private class CountingTopicGetter : IMessageTopicGetter<TestContext>
    {
        private readonly ITopic _topic;

        public CountingTopicGetter(ITopic topic) => _topic = topic;

        public int Calls { get; private set; }

        public ITopic? GetTopic(TestContext context)
        {
            Calls++;
            return _topic;
        }
    }

    private class StubBodyGetter : IMessageBodyGetter<TestContext>
    {
        public string? GetBody(TestContext context) => null;
    }

    private class StubHeadersGetter : IMessageHeadersGetter<TestContext>
    {
        public IDictionary<string, string> GetHeaders(TestContext context) => new Dictionary<string, string>();
    }

    private static MessageGetter<TestContext> Getter(CountingTopicGetter topicGetter, ResolvedTopicCache<TestContext>? cache) =>
        new(topicGetter, new StubBodyGetter(), new StubHeadersGetter(), cache);

    [Fact]
    public void GetTopic_WithCache_ExtractsFromTheTransportOnce_AcrossManyCalls()
    {
        var topicGetter = new CountingTopicGetter(new Topic("orders:create"));
        var getter = Getter(topicGetter, new ResolvedTopicCache<TestContext>());

        var results = new[] { getter.GetTopic(new TestContext()), getter.GetTopic(new TestContext()), getter.GetTopic(new TestContext()) };

        Assert.Equal(1, topicGetter.Calls); // ~a dozen consumers would otherwise each re-extract
        Assert.All(results, t => Assert.Equal("orders:create", t.Id));
    }

    [Fact]
    public void GetTopic_WithoutCache_ExtractsEveryCall_UnchangedBehaviour()
    {
        var topicGetter = new CountingTopicGetter(new Topic("orders:create"));
        var getter = Getter(topicGetter, cache: null);

        getter.GetTopic(new TestContext());
        getter.GetTopic(new TestContext());

        Assert.Equal(2, topicGetter.Calls);
    }

    [Fact]
    public async Task PresetTopicMiddleware_ResetsTheCache_SoTheRouterResolvesThePreset()
    {
        var cache = new ResolvedTopicCache<TestContext>();
        var transportTopicGetter = new CountingTopicGetter(new Topic("transport:topic"));
        var getter = Getter(transportTopicGetter, cache);

        // A tracing decorator resolves the topic BEFORE the preset middleware runs: caches the transport topic.
        Assert.Equal("transport:topic", getter.GetTopic(new TestContext()).Id);
        Assert.True(cache.HasValue);

        // The preset middleware applies the preset and must drop the stale cache.
        var middleware = new PresetTopicMiddleware<TestContext>(new PresetTopicHolder(), new Topic("preset:topic"), cache);
        await middleware.HandleAsync(new TestContext(), () => Task.CompletedTask);

        Assert.False(cache.HasValue); // reset, so the router re-resolves rather than serving the transport topic

        // The next resolution goes back to the getter (the real getter would now return the preset via
        // PresetTopicMessageTopicGetter; here we just prove the cache no longer short-circuits it).
        getter.GetTopic(new TestContext());
        Assert.Equal(2, transportTopicGetter.Calls);
    }
}
