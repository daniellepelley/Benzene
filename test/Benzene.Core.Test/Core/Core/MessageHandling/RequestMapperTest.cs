using System;
using System.Buffers;
using System.Text;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.Serialization;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class RequestMapperTest
{
    private class TestContext { }

    private class FakeBodyGetter : IMessageBodyGetter<TestContext>
    {
        public string Body { get; set; }
        public string GetBody(TestContext context) => Body;
    }

    private class FakeBodyBytesGetter : IMessageBodyBytesGetter<TestContext>
    {
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public ReadOnlyMemory<byte> GetBodyBytes(TestContext context) => Bytes;
    }

    private class PlainSerializer : ISerializer
    {
        public string Serialize(Type type, object payload) => payload?.ToString();
        public string Serialize<T>(T payload) => payload?.ToString();
        public object Deserialize(Type type, string payload) => new ExampleRequestPayload { Name = payload };
        public T Deserialize<T>(string payload) => (T)(object)new ExampleRequestPayload { Name = payload };
    }

    // Observable so tests can prove which path (string vs. bytes) the mapper actually took.
    private class RecordingPayloadSerializer : IPayloadSerializer
    {
        public bool StringPathCalled { get; private set; }
        public bool BytePathCalled { get; private set; }

        public string Serialize(Type type, object payload) => payload?.ToString();
        public string Serialize<T>(T payload) => payload?.ToString();

        public object Deserialize(Type type, string payload)
        {
            StringPathCalled = true;
            return new ExampleRequestPayload { Name = payload };
        }

        public T Deserialize<T>(string payload)
        {
            StringPathCalled = true;
            return (T)(object)new ExampleRequestPayload { Name = payload };
        }

        public void Serialize(Type type, object payload, IBufferWriter<byte> writer)
        {
        }

        public object Deserialize(Type type, ReadOnlySpan<byte> payload)
        {
            BytePathCalled = true;
            return new ExampleRequestPayload { Name = Encoding.UTF8.GetString(payload) };
        }
    }

    [Fact]
    public void GetBody_PrefersBytePath_WhenSerializerIsPayloadSerializerAndBytesGetterAvailable()
    {
        var serializer = new RecordingPayloadSerializer();
        var bodyGetter = new FakeBodyGetter { Body = "should-not-be-used" };
        var bytesGetter = new FakeBodyBytesGetter { Bytes = Encoding.UTF8.GetBytes("from-bytes") };

        var mapper = new RequestMapper<TestContext>(bodyGetter, serializer, bytesGetter);

        var result = mapper.GetBody<ExampleRequestPayload>(new TestContext());

        Assert.True(serializer.BytePathCalled);
        Assert.False(serializer.StringPathCalled);
        Assert.Equal("from-bytes", result.Name);
    }

    [Fact]
    public void GetBody_FallsBackToStringPath_WhenSerializerIsNotAPayloadSerializer()
    {
        var bodyGetter = new FakeBodyGetter { Body = "from-string" };
        var bytesGetter = new FakeBodyBytesGetter { Bytes = Encoding.UTF8.GetBytes("from-bytes") };

        var mapper = new RequestMapper<TestContext>(bodyGetter, new PlainSerializer(), bytesGetter);

        var result = mapper.GetBody<ExampleRequestPayload>(new TestContext());

        Assert.Equal("from-string", result.Name);
    }

    [Fact]
    public void GetBody_FallsBackToStringPath_WhenNoBytesGetterRegistered()
    {
        var serializer = new RecordingPayloadSerializer();
        var bodyGetter = new FakeBodyGetter { Body = "from-string" };

        var mapper = new RequestMapper<TestContext>(bodyGetter, serializer);

        var result = mapper.GetBody<ExampleRequestPayload>(new TestContext());

        Assert.True(serializer.StringPathCalled);
        Assert.False(serializer.BytePathCalled);
        Assert.Equal("from-string", result.Name);
    }

    [Fact]
    public void GetBody_EmptyBytes_ReturnsDefaultInstance()
    {
        var serializer = new RecordingPayloadSerializer();
        var mapper = new RequestMapper<TestContext>(new FakeBodyGetter(), serializer, new FakeBodyBytesGetter());

        var result = mapper.GetBody<ExampleRequestPayload>(new TestContext());

        Assert.NotNull(result);
        Assert.False(serializer.BytePathCalled);
        Assert.False(serializer.StringPathCalled);
    }
}
