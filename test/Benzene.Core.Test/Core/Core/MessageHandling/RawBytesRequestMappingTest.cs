using System;
using System.Text;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

/// <summary>
/// Covers the raw-bytes request passthrough added for #25/#28: a handler whose request type is
/// <see cref="RawBytesRequest"/>/<see cref="IRawBytesRequest"/> receives the request body's bytes
/// verbatim (from the byte getter, or the UTF-8 bytes of the string body as a fallback), while every
/// other request type still deserializes exactly as before.
/// </summary>
public class RawBytesRequestMappingTest
{
    public sealed class FakeContext;

    private sealed class StubBodyGetter : IMessageBodyGetter<FakeContext>
    {
        private readonly string _body;
        public StubBodyGetter(string body) => _body = body;
        public string GetBody(FakeContext context) => _body;
    }

    private sealed class StubBytesGetter : IMessageBodyBytesGetter<FakeContext>
    {
        private readonly byte[] _bytes;
        public StubBytesGetter(byte[] bytes) => _bytes = bytes;
        public ReadOnlyMemory<byte> GetBodyBytes(FakeContext context) => _bytes;
    }

    [Fact]
    public void GetBody_RawBytesRequest_WithBytesGetter_ReturnsRawBytesVerbatim()
    {
        var binary = new byte[] { 0x89, 0x50, 0x00, 0xFF, 0x1A };
        var mapper = new RequestMapper<FakeContext>(
            new StubBodyGetter("ignored"), new JsonSerializer(), new StubBytesGetter(binary));

        var request = mapper.GetBody<RawBytesRequest>(new FakeContext());

        Assert.NotNull(request);
        Assert.Equal(binary, request.Content.ToArray());
    }

    [Fact]
    public void GetBody_RawBytesRequest_WithoutBytesGetter_FallsBackToUtf8OfStringBody()
    {
        var mapper = new RequestMapper<FakeContext>(new StubBodyGetter("hello"), new JsonSerializer());

        var request = mapper.GetBody<RawBytesRequest>(new FakeContext());

        Assert.NotNull(request);
        Assert.Equal(Encoding.UTF8.GetBytes("hello"), request.Content.ToArray());
    }

    [Fact]
    public void GetBody_IRawBytesRequest_IsAlsoSatisfiedByThePassthrough()
    {
        var binary = new byte[] { 1, 2, 3 };
        var mapper = new RequestMapper<FakeContext>(
            new StubBodyGetter(""), new JsonSerializer(), new StubBytesGetter(binary));

        var request = mapper.GetBody<IRawBytesRequest>(new FakeContext());

        Assert.NotNull(request);
        Assert.Equal(binary, request.Content.ToArray());
    }

    [Fact]
    public void GetBody_NormalRequestType_StillDeserializes_WhenABytesGetterIsRegistered()
    {
        // Registering a bytes getter must not change how ordinary payloads map - they still
        // deserialize (here via the byte path, since JsonSerializer is an IPayloadSerializer).
        var json = new JsonSerializer().Serialize(new ExampleRequestPayload { Name = "some-name" });
        var mapper = new RequestMapper<FakeContext>(
            new StubBodyGetter(json), new JsonSerializer(), new StubBytesGetter(Encoding.UTF8.GetBytes(json)));

        var request = mapper.GetBody<ExampleRequestPayload>(new FakeContext());

        Assert.NotNull(request);
        Assert.Equal("some-name", request.Name);
    }
}
