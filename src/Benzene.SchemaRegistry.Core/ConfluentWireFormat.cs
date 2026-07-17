using System.Buffers;
using System.Buffers.Binary;

namespace Benzene.SchemaRegistry.Core;

/// <summary>
/// The Confluent Schema Registry wire format: a payload is prefixed with a <c>0x00</c> magic byte and
/// the 4-byte big-endian schema id, then the serialized body. This is the framing every Confluent
/// Kafka producer/consumer (and Avro/JSON/Protobuf deserializer) expects, so framing Benzene's
/// payloads this way makes them interoperable with the wider Kafka ecosystem — a non-Benzene consumer
/// can resolve the writer schema from the embedded id.
/// </summary>
public static class ConfluentWireFormat
{
    /// <summary>The leading magic byte identifying the Confluent wire format.</summary>
    public const byte MagicByte = 0x00;

    /// <summary>The number of framing bytes prepended to the payload (1 magic + 4 id).</summary>
    public const int HeaderLength = 5;

    /// <summary>Frames <paramref name="payload"/> with the magic byte and <paramref name="schemaId"/>.</summary>
    /// <param name="schemaId">The registry-wide schema id to embed.</param>
    /// <param name="payload">The serialized body.</param>
    /// <returns>The framed bytes.</returns>
    public static byte[] Encode(int schemaId, ReadOnlySpan<byte> payload)
    {
        var framed = new byte[HeaderLength + payload.Length];
        framed[0] = MagicByte;
        BinaryPrimitives.WriteInt32BigEndian(framed.AsSpan(1, 4), schemaId);
        payload.CopyTo(framed.AsSpan(HeaderLength));
        return framed;
    }

    /// <summary>Writes the framed representation of <paramref name="payload"/> to <paramref name="writer"/>.</summary>
    /// <param name="schemaId">The registry-wide schema id to embed.</param>
    /// <param name="payload">The serialized body.</param>
    /// <param name="writer">The buffer to write the framed bytes to.</param>
    public static void Encode(int schemaId, ReadOnlySpan<byte> payload, IBufferWriter<byte> writer)
    {
        var span = writer.GetSpan(HeaderLength + payload.Length);
        span[0] = MagicByte;
        BinaryPrimitives.WriteInt32BigEndian(span.Slice(1, 4), schemaId);
        payload.CopyTo(span.Slice(HeaderLength));
        writer.Advance(HeaderLength + payload.Length);
    }

    /// <summary>
    /// Reads the schema id from a framed message and returns the body after the header.
    /// </summary>
    /// <param name="framed">The framed bytes.</param>
    /// <param name="schemaId">Receives the embedded schema id.</param>
    /// <returns>The body bytes following the 5-byte header.</returns>
    /// <exception cref="FormatException">The buffer is too short or doesn't start with the magic byte.</exception>
    public static ReadOnlySpan<byte> Decode(ReadOnlySpan<byte> framed, out int schemaId)
    {
        if (framed.Length < HeaderLength)
        {
            throw new FormatException(
                $"Message is too short to be Confluent-framed ({framed.Length} bytes; need at least {HeaderLength}).");
        }

        if (framed[0] != MagicByte)
        {
            throw new FormatException(
                $"Message does not start with the Confluent magic byte 0x00 (got 0x{framed[0]:X2}).");
        }

        schemaId = BinaryPrimitives.ReadInt32BigEndian(framed.Slice(1, 4));
        return framed.Slice(HeaderLength);
    }
}
