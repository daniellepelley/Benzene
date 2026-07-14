using System;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;
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

    [Fact]
    public void GetBody_NoOptionMatches_UsesDefaultSerializer()
    {
        var resolver = ServiceResolverMother.CreateServiceResolver();

        var mapper = new MultiSerializerOptionsRequestMapper<TestContext, JsonSerializer>(
            resolver,
            new TestBodyGetter(),
            Array.Empty<ISerializerOption<TestContext>>(),
            Array.Empty<IRequestEnricher<TestContext>>());

        var context = new TestContext { Body = "{\"name\":\"foo\"}" };
        var result = mapper.GetBody<ExampleRequestPayload>(context);

        Assert.Equal("foo", result.Name);
    }

    [Fact]
    public void GetBody_RepeatedCallsForSameSelectedSerializer_ReuseTheSameUnderlyingSerializer()
    {
        var customSerializer = new CountingSerializer();
        var option = new InlineSerializerOption<TestContext>(context => context.SerializerKind == "custom", customSerializer);
        var resolver = ServiceResolverMother.CreateServiceResolver();

        var mapper = new MultiSerializerOptionsRequestMapper<TestContext, JsonSerializer>(
            resolver,
            new TestBodyGetter(),
            new ISerializerOption<TestContext>[] { option },
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

        var firstOption = new InlineSerializerOption<TestContext>(context => context.SerializerKind == "first", firstSerializer);
        var secondOption = new InlineSerializerOption<TestContext>(context => context.SerializerKind == "second", secondSerializer);
        var resolver = ServiceResolverMother.CreateServiceResolver();

        var mapper = new MultiSerializerOptionsRequestMapper<TestContext, JsonSerializer>(
            resolver,
            new TestBodyGetter(),
            new ISerializerOption<TestContext>[] { firstOption, secondOption },
            Array.Empty<IRequestEnricher<TestContext>>());

        mapper.GetBody<ExampleRequestPayload>(new TestContext { SerializerKind = "first", Body = "a" });
        mapper.GetBody<ExampleRequestPayload>(new TestContext { SerializerKind = "second", Body = "b" });

        Assert.Equal(1, firstSerializer.DeserializeCallCount);
        Assert.Equal(1, secondSerializer.DeserializeCallCount);
    }
}
