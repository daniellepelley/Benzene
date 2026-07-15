using System.Buffers;
using Avro.Generic;
using Avro.IO;
using Benzene.Abstractions.Serialization;

namespace Benzene.Avro;

/// <summary>
/// Apache Avro <see cref="IPayloadSerializer"/>. Avro is a binary wire format, so the byte-oriented
/// members (<see cref="Serialize(Type, object, IBufferWriter{byte})"/> /
/// <see cref="Deserialize(Type, ReadOnlySpan{byte})"/>) are the real, primary path — this is exactly
/// the byte path that <c>RequestMapper</c> takes when a transport registers
/// <c>IMessageBodyBytesGetter</c>. The string-based <see cref="ISerializer"/> members are a fallback
/// for string-only carriers (and the current response path, which serializes to a string): they
/// Base64-encode the same Avro binary rather than throwing, so Avro also works end-to-end over a
/// string body.
/// </summary>
public class AvroSerializer : ISerializer, IPayloadSerializer
{
    private readonly IAvroSchemaResolver _schemaResolver;

    /// <summary>Initializes a new instance backed by an explicit schema resolver.</summary>
    /// <param name="schemaResolver">Resolves the Avro schema for a CLR type.</param>
    public AvroSerializer(IAvroSchemaResolver schemaResolver)
    {
        _schemaResolver = schemaResolver;
    }

    /// <summary>Initializes a new instance from options (reflection schemas on by default).</summary>
    /// <param name="options">The Avro options controlling schema resolution.</param>
    public AvroSerializer(AvroOptions options) : this(new AvroSchemaResolver(options))
    {
    }

    /// <summary>Initializes a new instance with default options (reflection schemas).</summary>
    public AvroSerializer() : this(new AvroOptions())
    {
    }

    /// <inheritdoc />
    public void Serialize(Type type, object payload, IBufferWriter<byte> writer)
    {
        if (payload == null)
        {
            return;
        }

        var schema = _schemaResolver.GetSchema(type);
        var datum = AvroDatumConverter.ToDatum(schema, payload);
        var datumWriter = new GenericDatumWriter<object>(schema);

        using var memoryStream = new MemoryStream();
        var encoder = new BinaryEncoder(memoryStream);
        datumWriter.Write(datum, encoder);
        encoder.Flush();

        writer.Write(new ReadOnlySpan<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length));
    }

    /// <inheritdoc />
    public object? Deserialize(Type type, ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return null;
        }

        var schema = _schemaResolver.GetSchema(type);
        var datumReader = new GenericDatumReader<object>(schema, schema);

        using var memoryStream = new MemoryStream(payload.ToArray());
        var decoder = new BinaryDecoder(memoryStream);
        var datum = datumReader.Read(null!, decoder);

        return AvroDatumConverter.FromDatum(schema, datum, type);
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
    {
        return string.IsNullOrEmpty(payload) ? null : Deserialize(type, Convert.FromBase64String(payload));
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string payload)
    {
        var result = Deserialize(typeof(T), payload);
        return result == null ? default : (T)result;
    }
}
