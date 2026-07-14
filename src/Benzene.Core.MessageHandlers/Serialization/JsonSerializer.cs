using System.Buffers;
using System.Text.Json;
using Benzene.Abstractions.Serialization;

namespace Benzene.Core.MessageHandlers.Serialization;

/// <summary>
/// Default <see cref="ISerializer"/> implementation, backed by <see cref="System.Text.Json"/> with
/// camelCase property naming and case-insensitive deserialization. Registered as the process default
/// by <c>AddBenzene</c>; replace the <see cref="ISerializer"/> registration in DI to use a different
/// serialization format. Also implements <see cref="IPayloadSerializer"/> via
/// <see cref="Utf8JsonWriter"/>/<see cref="Utf8JsonReader"/>, so callers with byte-oriented access to
/// the request/response body (see <c>IMessageBodyBytesGetter{TContext}</c>) can skip the intermediate
/// string allocation the <see cref="ISerializer"/> members require.
/// </summary>
public class JsonSerializer : ISerializer, IPayloadSerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializer"/> class with the default options:
    /// camelCase property names and case-insensitive property matching on deserialization.
    /// </summary>
    public JsonSerializer()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializer"/> class with custom options.
    /// </summary>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization.</param>
    public JsonSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc />
    public virtual string Serialize(Type type, object payload)
    {
        return Serialize(payload);
    }

    /// <inheritdoc />
    public virtual string Serialize<T>(T payload)
    {
        return System.Text.Json.JsonSerializer.Serialize(payload, _jsonSerializerOptions);
    }

    /// <inheritdoc />
    public virtual object Deserialize(Type type, string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize(payload, type, _jsonSerializerOptions);
    }

    /// <inheritdoc />
    public virtual T Deserialize<T>(string payload)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(payload, _jsonSerializerOptions);
    }

    /// <inheritdoc />
    public virtual void Serialize(Type type, object payload, IBufferWriter<byte> writer)
    {
        using var utf8JsonWriter = new Utf8JsonWriter(writer);
        System.Text.Json.JsonSerializer.Serialize(utf8JsonWriter, payload, type, _jsonSerializerOptions);
    }

    /// <inheritdoc />
    public virtual object? Deserialize(Type type, ReadOnlySpan<byte> payload)
    {
        var utf8JsonReader = new Utf8JsonReader(payload);
        return System.Text.Json.JsonSerializer.Deserialize(ref utf8JsonReader, type, _jsonSerializerOptions);
    }
}
