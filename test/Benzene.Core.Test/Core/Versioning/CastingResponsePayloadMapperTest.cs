using System;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Results;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers;
using Benzene.Core.Messages;
using Benzene.Core.Versioning.Response;
using Benzene.Core.Versioning.Schemas;
using Benzene.Results;
using Xunit;
using V1 = Benzene.Test.Core.Versioning.Schemas.V1;
using V2 = Benzene.Test.Core.Versioning.Schemas.V2;

namespace Benzene.Test.Core.Versioning;

public class CastingResponsePayloadMapperTest
{
    public class TestContext
    {
    }

    private class RawResponse : IRawStringMessage
    {
        public string Content => "raw";
    }

    private class FakeResponsePayloadMapper : IResponsePayloadMapper<TestContext>
    {
        public IMessageHandlerResult? Captured { get; private set; }

        public string Map(TestContext context, IMessageHandlerResult messageHandlerResult, ISerializer serializer)
        {
            Captured = messageHandlerResult;
            return "SERIALIZED";
        }
    }

    private class NullSerializer : ISerializer
    {
        public string Serialize(Type type, object payload) => "";
        public string Serialize<T>(T payload) => "";
        public object? Deserialize(Type type, string payload) => null;
        public T? Deserialize<T>(string payload) => default;
    }

    private class FakeVersionGetter : IMessageVersionGetter<TestContext>
    {
        private readonly string? _version;
        public FakeVersionGetter(string? version) => _version = version;
        public string? GetVersion(TestContext context) => _version;
    }

    private class FakeTopicGetter : IMessageTopicGetter<TestContext>
    {
        private readonly string? _topic;
        public FakeTopicGetter(string? topic) => _topic = topic;
        public ITopic? GetTopic(TestContext context) => _topic == null ? null : new Topic(_topic);
    }

    private static ISchemaCasters Casters() =>
        new SchemaCasters(new SchemaCastersBuilder()
            .Add<V2.OrderPayload, V1.OrderPayload>("order", "V2", "V1")
            .Build());

    private static IMessageHandlerResult ResultWith(IBenzeneResult benzeneResult, Type responseType)
    {
        var definition = MessageHandlerDefinition.CreateInstance("order", typeof(object), responseType, typeof(object));
        return new MessageHandlerResult(new Topic("order"), definition, benzeneResult);
    }

    private static string Map(FakeResponsePayloadMapper inner, IMessageVersionGetter<TestContext> version, IMessageTopicGetter<TestContext> topic, ISchemaCasters? casters, IMessageHandlerResult result)
    {
        var mapper = new CastingResponsePayloadMapper<TestContext>(inner, version, topic, casters);
        return mapper.Map(new TestContext(), result, new NullSerializer());
    }

    [Fact]
    public void Map_SuccessWithRegisteredDowncaster_DowncastsToRequestedVersion()
    {
        var inner = new FakeResponsePayloadMapper();
        var result = ResultWith(BenzeneResult.Ok(new V2.OrderPayload { Id = "order-1", Quantity = 5, Currency = "USD" }), typeof(V2.OrderPayload));

        var returned = Map(inner, new FakeVersionGetter("V1"), new FakeTopicGetter("order"), Casters(), result);

        Assert.Equal("SERIALIZED", returned);
        // The inner mapper received a result reporting the DOWNCAST type and a V1 payload.
        Assert.Equal(typeof(V1.OrderPayload), inner.Captured!.MessageHandlerDefinition!.ResponseType);
        var payload = Assert.IsType<V1.OrderPayload>(inner.Captured.BenzeneResult.PayloadAsObject);
        Assert.Equal("order-1", payload.Id);
        Assert.Equal(5, payload.Quantity);
        // Status/success carried through unchanged.
        Assert.Equal(result.BenzeneResult.Status, inner.Captured.BenzeneResult.Status);
        Assert.True(inner.Captured.BenzeneResult.IsSuccessful);
    }

    [Fact]
    public void Map_FailureResult_PassesOriginalResultThrough()
    {
        var inner = new FakeResponsePayloadMapper();
        var result = ResultWith(BenzeneResult.ValidationError<V2.OrderPayload>("bad"), typeof(V2.OrderPayload));

        Map(inner, new FakeVersionGetter("V1"), new FakeTopicGetter("order"), Casters(), result);

        Assert.Same(result, inner.Captured);
    }

    [Fact]
    public void Map_NoVersionSignalled_PassesOriginalResultThrough()
    {
        var inner = new FakeResponsePayloadMapper();
        var result = ResultWith(BenzeneResult.Ok(new V2.OrderPayload { Id = "order-1" }), typeof(V2.OrderPayload));

        Map(inner, new FakeVersionGetter(null), new FakeTopicGetter("order"), Casters(), result);

        Assert.Same(result, inner.Captured);
    }

    [Fact]
    public void Map_NoSchemaCasters_PassesOriginalResultThrough()
    {
        var inner = new FakeResponsePayloadMapper();
        var result = ResultWith(BenzeneResult.Ok(new V2.OrderPayload { Id = "order-1" }), typeof(V2.OrderPayload));

        Map(inner, new FakeVersionGetter("V1"), new FakeTopicGetter("order"), null, result);

        Assert.Same(result, inner.Captured);
    }

    [Fact]
    public void Map_NoDowncasterForRequestedVersion_PassesOriginalResultThrough()
    {
        var inner = new FakeResponsePayloadMapper();
        var result = ResultWith(BenzeneResult.Ok(new V2.OrderPayload { Id = "order-1" }), typeof(V2.OrderPayload));

        Map(inner, new FakeVersionGetter("V9"), new FakeTopicGetter("order"), Casters(), result);

        Assert.Same(result, inner.Captured);
    }

    [Fact]
    public void Map_NullPayload_PassesOriginalResultThrough()
    {
        var inner = new FakeResponsePayloadMapper();
        var result = ResultWith(BenzeneResult.Ok<V2.OrderPayload>(), typeof(V2.OrderPayload));

        Map(inner, new FakeVersionGetter("V1"), new FakeTopicGetter("order"), Casters(), result);

        Assert.Same(result, inner.Captured);
    }

    [Fact]
    public void Map_RawStringPayload_PassesOriginalResultThrough()
    {
        var inner = new FakeResponsePayloadMapper();
        var result = ResultWith(BenzeneResult.Ok(new RawResponse()), typeof(RawResponse));

        Map(inner, new FakeVersionGetter("V1"), new FakeTopicGetter("order"), Casters(), result);

        Assert.Same(result, inner.Captured);
    }
}
