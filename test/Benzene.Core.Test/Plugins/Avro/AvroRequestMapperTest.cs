using System;
using System.Buffers;
using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Avro;
using Benzene.Core.MessageHandlers.Request;
using Xunit;

namespace Benzene.Test.Plugins.Avro;

/// <summary>
/// Proves Avro flows through the new request component: because <see cref="AvroSerializer"/> is an
/// <c>IPayloadSerializer</c> and a byte getter is supplied, <see cref="RequestMapper{TContext}"/>
/// takes the byte-oriented path and deserializes the raw Avro binary directly (no intermediate string).
/// </summary>
public class AvroRequestMapperTest
{
    private class FakeContext
    {
    }

    private class FakeBodyGetter : IMessageBodyGetter<FakeContext>
    {
        public string? GetBody(FakeContext context) => null;
    }

    private class FakeBytesGetter : IMessageBodyBytesGetter<FakeContext>
    {
        private readonly byte[] _bytes;

        public FakeBytesGetter(byte[] bytes) => _bytes = bytes;

        public ReadOnlyMemory<byte> GetBodyBytes(FakeContext context) => _bytes;
    }

    [Fact]
    public void RequestMapper_UsesBytePath_ForRawAvroBinary()
    {
        var serializer = new AvroSerializer();
        var order = new SampleOrderDto
        {
            Name = "MSFT",
            Quantity = 50,
            Price = 410.10m,
            Active = true,
            Id = Guid.NewGuid(),
            CreatedAt = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
            Status = SampleStatus.Pending,
            Tags = new List<string> { "tech" },
            Leg = new SampleLegDto { Label = "primary", Amount = 1 }
        };

        var buffer = new ArrayBufferWriter<byte>();
        serializer.Serialize(typeof(SampleOrderDto), order, buffer);
        var avroBytes = buffer.WrittenSpan.ToArray();

        var mapper = new RequestMapper<FakeContext>(
            new FakeBodyGetter(),
            serializer,
            new FakeBytesGetter(avroBytes));

        var result = mapper.GetBody<SampleOrderDto>(new FakeContext());

        Assert.NotNull(result);
        Assert.Equal("MSFT", result!.Name);
        Assert.Equal(50, result.Quantity);
        Assert.Equal(410.10m, result.Price);
        Assert.Equal(order.Id, result.Id);
        Assert.Equal(SampleStatus.Pending, result.Status);
        Assert.Equal(new List<string> { "tech" }, result.Tags);
    }
}
