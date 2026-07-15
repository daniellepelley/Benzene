using System;
using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.MediaFormats;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Extras.Request;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class MultiSerializerOptionsRequestMapperTest
{
    private class TestContext
    {
        public string Body { get; set; }
        public string SerializerKind { get; set; } = "default";
    }

    private class TestBodyGetter : IMessageBodyGetter<TestContext>
    {
        public string GetBody(TestContext context) => context.Body;
    }

    // A serializer whose Deserialize call is observable, so the test can prove which underlying
    // serializer actually ran without depending on JsonSerializer/XmlSerializer internals.
    private class CountingSerializer : ISerializer
    {
        public int DeserializeCallCount { get; private set; }

        public string Serialize(Type type, object payload) => payload?.ToString();

        public string Serialize<T>(T payload) => payload?.ToString();

        public object Deserialize(Type type, string payload)
        {
            DeserializeCallCount++;
            return new ExampleRequestPayload { Name = payload };
        }

        public T Deserialize<T>(string payload)
        {
            DeserializeCallCount++;
            return (T)(object)new ExampleRequestPayload { Name = payload };
        }
    }

    // Unlike the real MediaFormatNegotiator (scoped/memoizing - one selection per message), this
    // re-evaluates every call, so a single instance can be reused across multiple simulated
    // "messages" (contexts) within one test without an earlier selection leaking into a later one.
    private class RoutingMediaFormatNegotiator : IMediaFormatNegotiator<TestContext>
    {
        private readonly IMediaFormat<TestContext>[] _formats;
        private readonly IServiceResolver _serviceResolver;

        public RoutingMediaFormatNegotiator(IMediaFormat<TestContext>[] formats, IServiceResolver serviceResolver)
        {
            _formats = formats;
            _serviceResolver = serviceResolver;
        }

        public IMediaFormat<TestContext> SelectRead(TestContext context) =>
            _formats.First(format => format.CanRead(context, _serviceResolver));

        public IMediaFormat<TestContext> SelectWrite(TestContext context) => SelectRead(context);
    }

    [Fact]
    public void GetBody_NoOptionMatches_UsesDefaultSerializer()
    {
        var resolver = ServiceResolverMother.CreateServiceResolver();
        var mediaFormatNegotiator = new MediaFormatNegotiator<TestContext>(
            Array.Empty<IMediaFormat<TestContext>>(),
            new JsonMediaFormat<TestContext>(new JsonSerializer()),
            resolver);

        var mapper = new MultiSerializerOptionsRequestMapper<TestContext>(
            mediaFormatNegotiator,
            resolver,
            new TestBodyGetter(),
            Array.Empty<IRequestEnricher<TestContext>>());

        var context = new TestContext { Body = "{\"name\":\"foo\"}" };
        var result = mapper.GetBody<ExampleRequestPayload>(context);

        Assert.Equal("foo", result.Name);
    }

    [Fact]
    public void GetBody_RepeatedCallsForSameSelectedSerializer_ReuseTheSameUnderlyingSerializer()
    {
        var customSerializer = new CountingSerializer();
        var format = new InlineMediaFormat<TestContext>("application/custom", customSerializer,
            context => context.SerializerKind == "custom");
        var resolver = ServiceResolverMother.CreateServiceResolver();
        var mediaFormatNegotiator = new RoutingMediaFormatNegotiator(new IMediaFormat<TestContext>[] { format }, resolver);

        var mapper = new MultiSerializerOptionsRequestMapper<TestContext>(
            mediaFormatNegotiator,
            resolver,
            new TestBodyGetter(),
            Array.Empty<IRequestEnricher<TestContext>>());

        var context = new TestContext { SerializerKind = "custom", Body = "one" };

        var first = mapper.GetBody<ExampleRequestPayload>(context);
        var second = mapper.GetBody<ExampleRequestPayload>(context);

        Assert.Equal("one", first.Name);
        Assert.Equal("one", second.Name);
        Assert.Equal(2, customSerializer.DeserializeCallCount);
    }

    [Fact]
    public void GetBody_DifferentContextsSelectingDifferentOptions_EachRouteToItsOwnSerializer()
    {
        var firstSerializer = new CountingSerializer();
        var secondSerializer = new CountingSerializer();

        var firstFormat = new InlineMediaFormat<TestContext>("application/first", firstSerializer,
            context => context.SerializerKind == "first");
        var secondFormat = new InlineMediaFormat<TestContext>("application/second", secondSerializer,
            context => context.SerializerKind == "second");
        var resolver = ServiceResolverMother.CreateServiceResolver();
        var mediaFormatNegotiator = new RoutingMediaFormatNegotiator(
            new IMediaFormat<TestContext>[] { firstFormat, secondFormat }, resolver);

        var mapper = new MultiSerializerOptionsRequestMapper<TestContext>(
            mediaFormatNegotiator,
            resolver,
            new TestBodyGetter(),
            Array.Empty<IRequestEnricher<TestContext>>());

        mapper.GetBody<ExampleRequestPayload>(new TestContext { SerializerKind = "first", Body = "a" });
        mapper.GetBody<ExampleRequestPayload>(new TestContext { SerializerKind = "second", Body = "b" });

        Assert.Equal(1, firstSerializer.DeserializeCallCount);
        Assert.Equal(1, secondSerializer.DeserializeCallCount);
    }
}
