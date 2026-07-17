using System.Buffers;
using Benzene.Abstractions.Serialization;

namespace Benzene.SchemaRegistry.Core;

/// <summary>
/// An <see cref="IPayloadSerializer"/> decorator that frames an inner serializer's output with the
/// Confluent wire format (magic byte + schema id), so a Benzene producer's messages carry the schema
/// id the wider Kafka ecosystem resolves the writer schema from. Works over <em>any</em> inner
/// payload serializer (Avro, JSON, MessagePack) — it adds the registry framing, not a format.
/// </summary>
/// <remarks>
/// Schema ids are resolved once, at startup, into the id map this is constructed with (see
/// <see cref="SchemaRegistrar"/>), so serialization stays synchronous — no registry call on the hot
/// path. Serializing a type with no registered id throws, surfacing a missing startup registration
/// immediately. Like <c>Benzene.Avro</c>, the string members Base64-armor the framed bytes so the
/// serializer also flows through string-body pipelines unchanged.
/// </remarks>
public class SchemaRegistrySerializer : ISerializer, IPayloadSerializer
{
    private readonly IPayloadSerializer _inner;
    private readonly IReadOnlyDictionary<Type, int> _schemaIds;

    /// <summary>Initializes the decorator.</summary>
    /// <param name="inner">The serializer whose output is framed.</param>
    /// <param name="schemaIds">The per-type registered schema ids (built at startup).</param>
    public SchemaRegistrySerializer(IPayloadSerializer inner, IReadOnlyDictionary<Type, int> schemaIds)
    {
        _inner = inner;
        _schemaIds = schemaIds;
    }

    /// <inheritdoc />
    public string Serialize(Type type, object payload)
    {
        if (payload == null)
        {
            return string.Empty;
        }

        var buffer = new ArrayBufferWriter<byte>();
        Serialize(type, payload, buffer);
        return Convert.ToBase64String(buffer.WrittenSpan);
    }

    /// <inheritdoc />
    public string Serialize<T>(T payload) => payload == null ? string.Empty : Serialize(typeof(T), payload);

    /// <inheritdoc />
    public object? Deserialize(Type type, string payload)
        => string.IsNullOrEmpty(payload) ? null : Deserialize(type, Convert.FromBase64String(payload));

    /// <inheritdoc />
    public T? Deserialize<T>(string payload)
        => string.IsNullOrEmpty(payload) ? default : (T?)Deserialize(typeof(T), payload);

    /// <inheritdoc />
    public void Serialize(Type type, object payload, IBufferWriter<byte> writer)
    {
        if (payload == null)
        {
            return;
        }

        var body = new ArrayBufferWriter<byte>();
        _inner.Serialize(type, payload, body);
        ConfluentWireFormat.Encode(SchemaIdFor(type), body.WrittenSpan, writer);
    }

    /// <inheritdoc />
    public object? Deserialize(Type type, ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return null;
        }

        var body = ConfluentWireFormat.Decode(payload, out _);
        return _inner.Deserialize(type, body);
    }

    private int SchemaIdFor(Type type)
        => _schemaIds.TryGetValue(type, out var id)
            ? id
            : throw new InvalidOperationException(
                $"No schema id is registered for '{type.FullName}'. Register its schema at startup via SchemaRegistrar before serializing.");
}
