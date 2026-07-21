using System.Text;
using Avro.IO;
using AvroDecoder = global::Avro.IO.Decoder;

namespace Benzene.Avro;

/// <summary>
/// An Avro <see cref="Decoder"/> that wraps a <see cref="BinaryDecoder"/> and rejects a
/// length-prefixed <c>bytes</c>/<c>string</c> field whose declared length exceeds a bound, <em>before</em>
/// the underlying decoder allocates a buffer of that length. The bound is the decoded input size (no
/// legitimate field can be longer than the whole message) tightened by
/// <see cref="AvroOptions.MaxDeserializeBytes"/> when set. This stops the classic "tiny input declaring
/// a huge length prefix drives a large allocation" Avro OOM. Everything else delegates unchanged;
/// <c>fixed</c> fields are sized by the schema (not the payload) and array/map block counts are read as
/// blocks that fail at EOF, so neither is an unbounded single allocation.
/// </summary>
internal sealed class BoundedBinaryDecoder : AvroDecoder
{
    private readonly BinaryDecoder _inner;
    private readonly long _maxLength;

    public BoundedBinaryDecoder(BinaryDecoder inner, long maxLength)
    {
        _inner = inner;
        _maxLength = maxLength;
    }

    private int ReadGuardedLength()
    {
        var length = _inner.ReadLong();
        if (length < 0 || length > _maxLength)
        {
            throw new AvroPayloadTooLargeException(length, _maxLength);
        }

        return (int)length;
    }

    public byte[] ReadBytes()
    {
        // Avro `bytes` is [long length][length data]. Read the length, guard it, then read the data
        // via ReadFixed - replicating BinaryDecoder.ReadBytes but with the length check before allocation.
        var length = ReadGuardedLength();
        var buffer = new byte[length];
        _inner.ReadFixed(buffer, 0, length);
        return buffer;
    }

    public string ReadString()
    {
        // Avro `string` is [long length][length UTF-8 bytes] - same guard as ReadBytes.
        var length = ReadGuardedLength();
        var buffer = new byte[length];
        _inner.ReadFixed(buffer, 0, length);
        return Encoding.UTF8.GetString(buffer);
    }

    // Everything below delegates to the inner decoder unchanged.
    public void ReadNull() => _inner.ReadNull();
    public bool ReadBoolean() => _inner.ReadBoolean();
    public int ReadInt() => _inner.ReadInt();
    public long ReadLong() => _inner.ReadLong();
    public float ReadFloat() => _inner.ReadFloat();
    public double ReadDouble() => _inner.ReadDouble();
    public int ReadEnum() => _inner.ReadEnum();
    public long ReadArrayStart() => _inner.ReadArrayStart();
    public long ReadArrayNext() => _inner.ReadArrayNext();
    public long ReadMapStart() => _inner.ReadMapStart();
    public long ReadMapNext() => _inner.ReadMapNext();
    public int ReadUnionIndex() => _inner.ReadUnionIndex();
    public void ReadFixed(byte[] buffer) => _inner.ReadFixed(buffer);
    public void ReadFixed(byte[] buffer, int start, int length) => _inner.ReadFixed(buffer, start, length);
    public void SkipNull() => _inner.SkipNull();
    public void SkipBoolean() => _inner.SkipBoolean();
    public void SkipInt() => _inner.SkipInt();
    public void SkipLong() => _inner.SkipLong();
    public void SkipFloat() => _inner.SkipFloat();
    public void SkipDouble() => _inner.SkipDouble();
    public void SkipBytes() => _inner.SkipBytes();
    public void SkipString() => _inner.SkipString();
    public void SkipEnum() => _inner.SkipEnum();
    public void SkipUnionIndex() => _inner.SkipUnionIndex();
    public void SkipFixed(int len) => _inner.SkipFixed(len);
}
