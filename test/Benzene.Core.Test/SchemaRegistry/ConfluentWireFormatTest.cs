using System;
using System.Buffers;
using System.Text;
using Benzene.SchemaRegistry.Core;
using Xunit;

namespace Benzene.Test.SchemaRegistry;

// The interop-critical byte framing: magic 0x00 + 4-byte big-endian schema id + body.
public class ConfluentWireFormatTest
{
    [Fact]
    public void Encode_ProducesMagicByte_BigEndianId_ThenBody()
    {
        var body = Encoding.UTF8.GetBytes("hello");

        var framed = ConfluentWireFormat.Encode(schemaId: 258, body); // 258 = 0x00000102

        Assert.Equal(ConfluentWireFormat.MagicByte, framed[0]);
        Assert.Equal(new byte[] { 0x00, 0x00, 0x01, 0x02 }, framed[1..5]); // big-endian
        Assert.Equal(body, framed[5..]);
    }

    [Fact]
    public void Decode_RecoversIdAndBody_RoundTrip()
    {
        var body = Encoding.UTF8.GetBytes("payload");
        var framed = ConfluentWireFormat.Encode(42, body);

        var decoded = ConfluentWireFormat.Decode(framed, out var schemaId);

        Assert.Equal(42, schemaId);
        Assert.Equal(body, decoded.ToArray());
    }

    [Fact]
    public void Encode_ToBufferWriter_MatchesArrayEncode()
    {
        var body = Encoding.UTF8.GetBytes("abc");
        var writer = new ArrayBufferWriter<byte>();

        ConfluentWireFormat.Encode(7, body, writer);

        Assert.Equal(ConfluentWireFormat.Encode(7, body), writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void Decode_TooShort_Throws()
    {
        Assert.Throws<FormatException>(() => ConfluentWireFormat.Decode(new byte[] { 0x00, 0x01 }, out _));
    }

    [Fact]
    public void Decode_WrongMagicByte_Throws()
    {
        var notFramed = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x01, 0xAA };
        Assert.Throws<FormatException>(() => ConfluentWireFormat.Decode(notFramed, out _));
    }
}
