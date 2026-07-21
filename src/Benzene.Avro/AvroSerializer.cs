using System.Buffers;
using System.Text;
using Avro.Generic;
using Avro.IO;
using Benzene.Abstractions.Serialization;

namespace Benzene.Avro;

/// <summary>
/// Apache Avro <see cref="IPayloadSerializer"/>. The wire representation is genuine Avro binary, but
/// every Benzene transport's request/response body is a <see cref="string"/> today (even
/// <c>BenzeneMessageContext</c>'s byte-oriented path is "UTF-8 bytes of the string body", not a true
/// arbitrary-binary carrier). So, like <c>Benzene.MessagePack</c>, this Base64-armors the Avro bytes
/// rather than making the string members throw: <see cref="Serialize(Type, object)"/> produces Base64
/// text and <see cref="Deserialize(Type, string)"/> consumes it, so Avro works unchanged through every
/// existing string pipeline. The byte-oriented members delegate through the same Base64 representation
/// (UTF-8 bytes of the Base64 text), staying consistent with the string path while still exercising
/// the byte-oriented request-mapping path wherever an <c>IMessageBodyBytesGetter{TContext}</c> is
/// registered.
/// </summary>
public class AvroSerializer : ISerializer, IPayloadSerializer
{
    private readonly IAvroSchemaResolver _schemaResolver;
    private readonly long? _maxDeserializeBytes;

    /// <summary>Initializes a new instance backed by an explicit schema resolver.</summary>
    /// <param name="schemaResolver">Resolves the Avro schema for a CLR type.</param>
    /// <param name="maxDeserializeBytes">
    /// Optional tighter cap on any single length-prefixed field on deserialize
    /// (see <see cref="AvroOptions.MaxDeserializeBytes"/>); the input-size bound always applies.
    /// </param>
    public AvroSerializer(IAvroSchemaResolver schemaResolver, long? maxDeserializeBytes = null)
    {
        _schemaResolver = schemaResolver;
        _maxDeserializeBytes = maxDeserializeBytes;
    }

    /// <summary>Initializes a new instance from options (reflection schemas on by default).</summary>
    /// <param name="options">The Avro options controlling schema resolution.</param>
    public AvroSerializer(AvroOptions options) : this(new AvroSchemaResolver(options), options.MaxDeserializeBytes)
    {
    }

    /// <summary>Initializes a new instance with default options (reflection schemas).</summary>
    public AvroSerializer() : this(new AvroOptions())
    {
    }

    /// <inheritdoc />
    public string Serialize(Type type, object payload)
    {
        return payload == null ? string.Empty : Convert.ToBase64String(SerializeToAvroBytes(type, payload));
    }

    /// <inheritdoc />
    public string Serialize<T>(T payload) => payload == null ? string.Empty : Serialize(typeof(T), payload);

    /// <inheritdoc />
    public object? Deserialize(Type type, string payload)
    {
        return string.IsNullOrEmpty(payload) ? null : DeserializeFromAvroBytes(type, Convert.FromBase64String(payload));
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string payload)
    {
        var result = Deserialize(typeof(T), payload);
        return result == null ? default : (T)result;
    }

    /// <inheritdoc />
    public void Serialize(Type type, object payload, IBufferWriter<byte> writer)
    {
        writer.Write(Encoding.UTF8.GetBytes(Serialize(type, payload)));
    }

    /// <inheritdoc />
    public object? Deserialize(Type type, ReadOnlySpan<byte> payload)
    {
        return Deserialize(type, Encoding.UTF8.GetString(payload));
    }

    private byte[] SerializeToAvroBytes(Type type, object payload)
    {
        var schema = _schemaResolver.GetSchema(type);
        var datum = AvroDatumConverter.ToDatum(schema, payload);
        var datumWriter = new GenericDatumWriter<object>(schema);

        using var memoryStream = new MemoryStream();
        var encoder = new BinaryEncoder(memoryStream);
        datumWriter.Write(datum, encoder);
        encoder.Flush();

        return memoryStream.ToArray();
    }

    private object? DeserializeFromAvroBytes(Type type, ReadOnlySpan<byte> avroBytes)
    {
        if (avroBytes.IsEmpty)
        {
            return null;
        }

        var schema = _schemaResolver.GetSchema(type);
        var datumReader = new GenericDatumReader<object>(schema, schema);

        using var memoryStream = new MemoryStream(avroBytes.ToArray());
        // Bound every length-prefixed field at the input size (no legitimate field can be longer than
        // the whole message), tightened by MaxDeserializeBytes when set. This rejects a hostile
        // length prefix before the underlying BinaryDecoder allocates a buffer of that size.
        var maxLength = _maxDeserializeBytes.HasValue
            ? System.Math.Min(_maxDeserializeBytes.Value, avroBytes.Length)
            : avroBytes.Length;
        var decoder = new BoundedBinaryDecoder(new BinaryDecoder(memoryStream), maxLength);
        var datum = datumReader.Read(null!, decoder);

        return AvroDatumConverter.FromDatum(schema, datum, type);
    }
}
