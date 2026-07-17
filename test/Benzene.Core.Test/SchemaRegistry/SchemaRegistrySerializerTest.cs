using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Benzene.Abstractions.Serialization;
using Benzene.SchemaRegistry.Core;
using Xunit;

namespace Benzene.Test.SchemaRegistry;

public class SchemaRegistrySerializerTest
{
    // A trivial inner serializer (payload is a string, wire form is its UTF-8 bytes) so the framing
    // decorator can be tested without pulling in Avro.
    private class Utf8StringSerializer : ISerializer, IPayloadSerializer
    {
        public string Serialize(Type type, object payload) => (string)payload;
        public string Serialize<T>(T payload) => (string)(object)payload!;
        public object? Deserialize(Type type, string payload) => payload;
        public T? Deserialize<T>(string payload) => (T?)(object?)payload;

        public void Serialize(Type type, object payload, IBufferWriter<byte> writer)
            => writer.Write(Encoding.UTF8.GetBytes((string)payload));

        public object? Deserialize(Type type, ReadOnlySpan<byte> payload) => Encoding.UTF8.GetString(payload);
    }

    private static SchemaRegistrar Registrar(ISchemaRegistryClient registry)
        => new(registry, new DelegateSchemaResolver(t => new SchemaDefinition($"{t.Name}-value", "{\"type\":\"string\"}")));

    [Fact]
    public async Task Registrar_RegistersSchemas_AndSerializerFramesWithThatId()
    {
        var registry = new InMemorySchemaRegistryClient(SchemaCompatibilityMode.None);
        var serializer = await Registrar(registry).CreateSerializerAsync(new Utf8StringSerializer(), new[] { typeof(string) });

        var buffer = new ArrayBufferWriter<byte>();
        serializer.Serialize(typeof(string), "hello", buffer);

        // Resolve the registered id before decoding, so no Span is held across the await.
        var registered = await registry.GetLatestAsync("String-value");
        var body = ConfluentWireFormat.Decode(buffer.WrittenSpan, out var schemaId);

        Assert.Equal(registered!.Id, schemaId);         // framed with the registered id
        Assert.Equal("hello", Encoding.UTF8.GetString(body));
    }

    [Fact]
    public async Task Serializer_StringPath_RoundTripsThroughBase64Frame()
    {
        var registry = new InMemorySchemaRegistryClient(SchemaCompatibilityMode.None);
        var serializer = await Registrar(registry).CreateSerializerAsync(new Utf8StringSerializer(), new[] { typeof(string) });

        var text = serializer.Serialize(typeof(string), "hi");   // Base64 of the framed bytes
        var back = serializer.Deserialize(typeof(string), text);

        Assert.Equal("hi", back);
    }

    [Fact]
    public void Serializer_UnregisteredType_Throws()
    {
        var serializer = new SchemaRegistrySerializer(new Utf8StringSerializer(), new Dictionary<Type, int>());
        var buffer = new ArrayBufferWriter<byte>();

        Assert.Throws<InvalidOperationException>(() => serializer.Serialize(typeof(string), "x", buffer));
    }

    [Fact]
    public async Task EnsureCompatible_Throws_WhenSchemaChangedUnderBackward()
    {
        var registry = new InMemorySchemaRegistryClient(SchemaCompatibilityMode.Backward);
        await registry.RegisterAsync(new SchemaDefinition("String-value", "v1"));

        // Resolver now yields a different schema for the same subject -> incompatible under Backward.
        var registrar = new SchemaRegistrar(registry, new DelegateSchemaResolver(_ => new SchemaDefinition("String-value", "v2")));

        await Assert.ThrowsAsync<SchemaIncompatibleException>(
            () => registrar.EnsureCompatibleAsync(new[] { typeof(string) }));
    }
}
