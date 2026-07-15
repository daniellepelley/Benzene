using System;
using System.Buffers;
using System.Text;
using Benzene.MessagePack;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Plugins.MessagePack;

public class MessagePackSerializerTest
{
    [Fact]
    public void Serialization_RoundTrips()
    {
        var payload = new ExampleRequestPayload { Id = 42, Name = "foo" };

        var serializer = new MessagePackSerializer();

        var base64 = serializer.Serialize(payload);
        var result = serializer.Deserialize<ExampleRequestPayload>(base64);

        Assert.Equal(payload.Id, result.Id);
        Assert.Equal(payload.Name, result.Name);
    }

    [Fact]
    public void Serialize_NullPayload_ReturnsEmptyString()
    {
        var serializer = new MessagePackSerializer();

        Assert.Equal(string.Empty, serializer.Serialize<ExampleRequestPayload>(null));
    }

    [Fact]
    public void Deserialize_EmptyPayload_ReturnsNull()
    {
        var serializer = new MessagePackSerializer();

        Assert.Null(serializer.Deserialize<ExampleRequestPayload>(string.Empty));
    }

    [Fact]
    public void BytePath_RoundTrips()
    {
        var payload = new ExampleRequestPayload { Id = 42, Name = "foo" };
        var serializer = new MessagePackSerializer();

        var writer = new ArrayBufferWriter<byte>();
        serializer.Serialize(typeof(ExampleRequestPayload), payload, writer);
        var result = (ExampleRequestPayload)serializer.Deserialize(typeof(ExampleRequestPayload), writer.WrittenSpan);

        Assert.Equal(payload.Id, result.Id);
        Assert.Equal(payload.Name, result.Name);
    }

    [Fact]
    public void BytePathAndStringPath_ProduceTheSameBase64Text()
    {
        var payload = new ExampleRequestPayload { Id = 42, Name = "foo" };
        var serializer = new MessagePackSerializer();

        var expected = serializer.Serialize(payload);

        var writer = new ArrayBufferWriter<byte>();
        serializer.Serialize(typeof(ExampleRequestPayload), payload, writer);
        var actual = Encoding.UTF8.GetString(writer.WrittenSpan);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Serialize_ProducesBase64Text_NotRawBytes()
    {
        // The whole point of the Base64-armoring design: the string output must be valid Base64,
        // decodable back into the raw msgpack bytes independently of this serializer.
        var payload = new ExampleRequestPayload { Id = 1, Name = "bar" };
        var serializer = new MessagePackSerializer();

        var base64 = serializer.Serialize(payload);
        var rawBytes = Convert.FromBase64String(base64);

        Assert.NotEmpty(rawBytes);
    }
}
