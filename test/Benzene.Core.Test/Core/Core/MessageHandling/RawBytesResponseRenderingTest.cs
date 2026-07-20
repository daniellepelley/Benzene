using System;
using System.Threading.Tasks;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.MessageHandlers.MediaFormats;
using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers.Response;
using Benzene.Core.Messages;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

/// <summary>
/// Covers the raw-binary response path added for #25(B)/#28(c): when a handler's payload implements
/// <see cref="Benzene.Abstractions.Messages.IRawBytesMessage"/>, <see cref="SerializerResponseRenderer{TContext}"/>
/// writes the bytes verbatim via the byte-oriented <c>SetBody</c> overload and bypasses serialization
/// and format negotiation entirely.
/// </summary>
public class RawBytesResponseRenderingTest
{
    public sealed class FakeContext;

    private sealed class RecordingResponseAdapter : IBenzeneResponseAdapter<FakeContext>
    {
        public byte[] BytesBody { get; private set; }
        public string StringBody { get; private set; }
        public string ContentType { get; private set; }

        public void SetResponseHeader(FakeContext context, string headerKey, string headerValue) { }
        public void SetContentType(FakeContext context, string contentType) => ContentType = contentType;
        public void SetStatusCode(FakeContext context, string statusCode) { }
        public void SetBody(FakeContext context, string body) => StringBody = body;
        public void SetBody(FakeContext context, ReadOnlyMemory<byte> body) => BytesBody = body.ToArray();
        public string GetBody(FakeContext context) => StringBody;
        public Task FinalizeAsync(FakeContext context) => Task.CompletedTask;
    }

    [Fact]
    public async Task RenderAsync_RawBytesPayload_WritesBytesVerbatim_AndBypassesSerialization()
    {
        var payloadBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x00, 0xFF };
        var mockResult = new Mock<IMessageHandlerResult>();
        mockResult.Setup(x => x.BenzeneResult).Returns(
            Mock.Of<IBenzeneResult>(r => r.PayloadAsObject == new RawBytesMessage(payloadBytes, "image/png")));

        var mockPayloadMapper = new Mock<IResponsePayloadMapper<FakeContext>>();
        var mockNegotiator = new Mock<IMediaFormatNegotiator<FakeContext>>();

        var renderer = new SerializerResponseRenderer<FakeContext>(
            mockPayloadMapper.Object, mockNegotiator.Object, Mock.Of<IServiceResolver>());

        var adapter = new RecordingResponseAdapter();
        await renderer.RenderAsync(new FakeContext(), mockResult.Object, adapter);

        Assert.Equal(payloadBytes, adapter.BytesBody);
        Assert.Equal("image/png", adapter.ContentType);
        Assert.Null(adapter.StringBody);
        // Serialization + negotiation were bypassed entirely.
        mockNegotiator.Verify(x => x.SelectWrite(It.IsAny<FakeContext>()), Times.Never);
        mockPayloadMapper.Verify(x => x.Map(It.IsAny<FakeContext>(), It.IsAny<IMessageHandlerResult>(), It.IsAny<Benzene.Abstractions.Serialization.ISerializer>()), Times.Never);
    }
}
