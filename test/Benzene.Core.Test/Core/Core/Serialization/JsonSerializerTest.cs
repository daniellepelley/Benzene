using System.Buffers;
using System.Text;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Core.Serialization;

public class JsonSerializerTest
{
    [Fact]
    public void Serialize_ByteAndStringPaths_ProduceByteIdenticalJson()
    {
        var serializer = new JsonSerializer();
        var payload = new ExampleRequestPayload { Id = 42, Name = "some-name" };

        var expected = serializer.Serialize(typeof(ExampleRequestPayload), payload);

        var writer = new ArrayBufferWriter<byte>();
        serializer.Serialize(typeof(ExampleRequestPayload), payload, writer);
        var actual = Encoding.UTF8.GetString(writer.WrittenSpan);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Deserialize_ByteAndStringPaths_ProduceEquivalentObjects()
    {
        var serializer = new JsonSerializer();
        var json = serializer.Serialize(new ExampleRequestPayload { Id = 42, Name = "some-name" });

        var viaString = serializer.Deserialize<ExampleRequestPayload>(json);
        var viaBytes = (ExampleRequestPayload)serializer.Deserialize(typeof(ExampleRequestPayload), Encoding.UTF8.GetBytes(json));

        Assert.Equal(viaString.Id, viaBytes.Id);
        Assert.Equal(viaString.Name, viaBytes.Name);
    }

    [Fact]
    public void JsonSerializer_ImplementsIPayloadSerializer()
    {
        Assert.IsAssignableFrom<Benzene.Abstractions.Serialization.IPayloadSerializer>(new JsonSerializer());
    }
}
