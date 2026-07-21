using System.Collections.Generic;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Idempotency;
using Xunit;

namespace Benzene.Test.Idempotency;

public class HeaderOrBodyHashIdempotencyKeyStrategyTest
{
    private class Ctx
    {
        public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
        public string? Body { get; init; }
        public ITopic? Topic { get; init; }
    }

    private class Topic : ITopic
    {
        public Topic(string id, string version) { Id = id; Version = version; }
        public string Id { get; }
        public string Version { get; }
    }

    private class HeadersGetter : IMessageHeadersGetter<Ctx>
    {
        public IDictionary<string, string> GetHeaders(Ctx context) => context.Headers;
    }

    private class BodyGetter : IMessageBodyGetter<Ctx>
    {
        public string? GetBody(Ctx context) => context.Body;
    }

    private class TopicGetter : IMessageTopicGetter<Ctx>
    {
        public ITopic? GetTopic(Ctx context) => context.Topic;
    }

    private static HeaderOrBodyHashIdempotencyKeyStrategy<Ctx> Strategy(IdempotencyOptions? options = null)
        => new(new HeadersGetter(), new BodyGetter(), new TopicGetter(), options ?? new IdempotencyOptions());

    [Fact]
    public void Uses_HeaderKey_WhenPresent()
    {
        var ctx = new Ctx { Headers = new Dictionary<string, string> { ["idempotency-key"] = "abc-123" } };

        Assert.Equal("abc-123", Strategy().GetKey(ctx));
    }

    [Fact]
    public void Uses_HeaderKey_RegardlessOfHeaderKeyCasing()
    {
        // wire-contracts.md §2: header keys are case-insensitive on read, regardless of the header
        // dictionary's comparer. A canonically-cased "Idempotency-Key" must still be honoured rather
        // than silently falling through to body-hashing (which would ignore the caller's explicit key).
        var ctx = new Ctx
        {
            Headers = new Dictionary<string, string> { ["Idempotency-Key"] = "abc-123" },
            Body = "{\"id\":1}",
            Topic = new Topic("order:create", "1")
        };

        Assert.Equal("abc-123", Strategy().GetKey(ctx));
    }

    [Fact]
    public void RespectsKeyPrefix()
    {
        var ctx = new Ctx { Headers = new Dictionary<string, string> { ["idempotency-key"] = "abc-123" } };

        var key = Strategy(new IdempotencyOptions { KeyPrefix = "orders:" }).GetKey(ctx);

        Assert.Equal("orders:abc-123", key);
    }

    [Fact]
    public void HashesTopicAndBody_WhenNoHeader()
    {
        var ctx = new Ctx { Body = "{\"id\":1}", Topic = new Topic("order:create", "1") };

        var key = Strategy().GetKey(ctx);

        Assert.NotNull(key);
        Assert.NotEqual("", key);
    }

    [Fact]
    public void SameTopicAndBody_ProduceSameKey()
    {
        var a = new Ctx { Body = "{\"id\":1}", Topic = new Topic("order:create", "1") };
        var b = new Ctx { Body = "{\"id\":1}", Topic = new Topic("order:create", "1") };

        Assert.Equal(Strategy().GetKey(a), Strategy().GetKey(b));
    }

    [Fact]
    public void DifferentBody_ProducesDifferentKey()
    {
        var a = new Ctx { Body = "{\"id\":1}", Topic = new Topic("order:create", "1") };
        var b = new Ctx { Body = "{\"id\":2}", Topic = new Topic("order:create", "1") };

        Assert.NotEqual(Strategy().GetKey(a), Strategy().GetKey(b));
    }

    [Fact]
    public void ReturnsNull_WhenNoHeader_AndBodyHashingDisabled()
    {
        var ctx = new Ctx { Body = "{\"id\":1}", Topic = new Topic("order:create", "1") };

        var key = Strategy(new IdempotencyOptions { HashBodyWhenNoHeader = false }).GetKey(ctx);

        Assert.Null(key);
    }

    [Fact]
    public void DistinctTopicTriples_ThatShareASeparatorFlattening_ProduceDifferentKeys()
    {
        // Two genuinely different messages whose (id, version) split point differs but whose naive
        // concatenation is identical: id="order"/version="v2:create" vs id="order:v2"/version="create".
        // These must NOT collide, or one is silently dropped as a false duplicate of the other.
        var a = new Ctx { Body = "{}", Topic = new Topic("order", "v2:create") };
        var b = new Ctx { Body = "{}", Topic = new Topic("order:v2", "create") };

        Assert.NotEqual(Strategy().GetKey(a), Strategy().GetKey(b));
    }
}
