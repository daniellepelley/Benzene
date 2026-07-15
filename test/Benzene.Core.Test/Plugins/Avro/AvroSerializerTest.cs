using System;
using System.Buffers;
using System.Collections.Generic;
using Benzene.Abstractions.Serialization;
using Benzene.Avro;
using Xunit;

namespace Benzene.Test.Plugins.Avro;

public class AvroSerializerTest
{
    private static SampleOrderDto CreateSample() => new()
    {
        Name = "AAPL",
        Quantity = 100,
        Reference = 9_000_000_001L,
        Price = 187.42m,
        Weight = 12.5,
        Active = true,
        Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        CreatedAt = new DateTime(2026, 7, 15, 9, 30, 0, DateTimeKind.Utc),
        Status = SampleStatus.Filled,
        Tags = new List<string> { "equity", "us" },
        OptionalCount = 7,
        Leg = new SampleLegDto { Label = "leg-1", Amount = 42.75 }
    };

    private static void AssertEqual(SampleOrderDto expected, SampleOrderDto actual)
    {
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Quantity, actual.Quantity);
        Assert.Equal(expected.Reference, actual.Reference);
        Assert.Equal(expected.Price, actual.Price);
        Assert.Equal(expected.Weight, actual.Weight);
        Assert.Equal(expected.Active, actual.Active);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.Tags, actual.Tags);
        Assert.Equal(expected.OptionalCount, actual.OptionalCount);
        Assert.Equal(expected.Leg.Label, actual.Leg.Label);
        Assert.Equal(expected.Leg.Amount, actual.Leg.Amount);
    }

    [Fact]
    public void Implements_IPayloadSerializer()
    {
        Assert.IsAssignableFrom<IPayloadSerializer>(new AvroSerializer());
    }

    [Fact]
    public void BytePath_RoundTrips_AllSupportedTypes()
    {
        var serializer = new AvroSerializer();
        var sample = CreateSample();

        var buffer = new ArrayBufferWriter<byte>();
        serializer.Serialize(typeof(SampleOrderDto), sample, buffer);
        var bytes = buffer.WrittenSpan.ToArray();

        Assert.NotEmpty(bytes);

        var result = (SampleOrderDto?)serializer.Deserialize(typeof(SampleOrderDto), bytes);

        Assert.NotNull(result);
        AssertEqual(sample, result!);
    }

    [Fact]
    public void StringPath_RoundTrips_ViaBase64()
    {
        var serializer = new AvroSerializer();
        var sample = CreateSample();

        var encoded = serializer.Serialize(sample);

        // The string path Base64-encodes the Avro binary, so it decodes cleanly as Base64.
        var decodedBytes = Convert.FromBase64String(encoded);
        Assert.NotEmpty(decodedBytes);

        var result = serializer.Deserialize<SampleOrderDto>(encoded);

        Assert.NotNull(result);
        AssertEqual(sample, result!);
    }

    [Fact]
    public void BytePathAndStringPath_ProduceTheSameBase64Text()
    {
        var serializer = new AvroSerializer();
        var sample = CreateSample();

        var expected = serializer.Serialize(sample);

        var buffer = new ArrayBufferWriter<byte>();
        serializer.Serialize(typeof(SampleOrderDto), sample, buffer);
        var actual = System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);

        // The byte path is the UTF-8 bytes of the same Base64 text the string path returns.
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Serialize_ProducesBase64Text_DecodableToRealAvroBytes()
    {
        var serializer = new AvroSerializer();

        var base64 = serializer.Serialize(CreateSample());
        var avroBytes = Convert.FromBase64String(base64);

        Assert.NotEmpty(avroBytes);
    }

    [Fact]
    public void Serialize_NullPayload_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, new AvroSerializer().Serialize<SampleOrderDto>(null!));
    }

    [Fact]
    public void Deserialize_EmptyPayload_ReturnsNull()
    {
        Assert.Null(new AvroSerializer().Deserialize<SampleOrderDto>(string.Empty));
    }

    [Fact]
    public void NullableAndNullMembers_RoundTrip()
    {
        var serializer = new AvroSerializer();
        var sample = CreateSample();
        sample.OptionalCount = null;
        sample.Tags = new List<string>();

        var buffer = new ArrayBufferWriter<byte>();
        serializer.Serialize(typeof(SampleOrderDto), sample, buffer);
        var result = (SampleOrderDto?)serializer.Deserialize(typeof(SampleOrderDto), buffer.WrittenSpan.ToArray());

        Assert.NotNull(result);
        Assert.Null(result!.OptionalCount);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public void ExplicitSchema_IsUsedWhenRegistered()
    {
        const string pointSchema =
            "{\"type\":\"record\",\"name\":\"Point\",\"fields\":[" +
            "{\"name\":\"X\",\"type\":\"int\"},{\"name\":\"Y\",\"type\":\"int\"}]}";

        var serializer = new AvroSerializer(new AvroOptions().RegisterSchema<Point>(pointSchema));
        var point = new Point { X = 3, Y = 4 };

        var buffer = new ArrayBufferWriter<byte>();
        serializer.Serialize(typeof(Point), point, buffer);
        var result = (Point?)serializer.Deserialize(typeof(Point), buffer.WrittenSpan.ToArray());

        Assert.NotNull(result);
        Assert.Equal(3, result!.X);
        Assert.Equal(4, result.Y);
    }

    [Fact]
    public void ReflectionDisabled_WithoutRegisteredSchema_Throws()
    {
        var serializer = new AvroSerializer(new AvroOptions { UseReflectionSchemas = false });

        Assert.Throws<InvalidOperationException>(() => serializer.Serialize(new Point { X = 1, Y = 2 }));
    }
}
