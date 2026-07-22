using System.Buffers;
using System.Text.Encodings.Web;
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
/// <remarks>
/// The default options use <see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/>. Benzene emits
/// JSON to API clients (and to browsers hitting an endpoint directly), never HTML, so the standard
/// encoder's HTML-safety escaping only hurts here: characters like <c>&lt;</c>, <c>&gt;</c>,
/// <c>&amp;</c> and <c>'</c> would otherwise render as <c>\uXXXX</c> escape sequences in the body -
/// e.g. a framework error detail carrying the <c>&lt;missing&gt;</c> topic sentinel comes out as an
/// unreadable escape run. The relaxed encoder writes those characters literally so response bodies
/// read cleanly on the wire. This is the Microsoft-recommended setting for JSON that isn't
/// embedded in HTML; if you serve Benzene JSON into an HTML context unescaped, supply your own
/// <see cref="JsonSerializerOptions"/> via the other constructor.
/// </remarks>
public class JsonSerializer : ISerializer, IPayloadSerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializer"/> class with the default options:
    /// camelCase property names, case-insensitive property matching on deserialization, and relaxed
    /// (non-HTML) character escaping so wire messages render without <c>\uXXXX</c> sequences.
    /// </summary>
    public JsonSerializer()
    {
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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
        // The Utf8JsonWriter's own options - not _jsonSerializerOptions - govern character escaping,
        // so carry the configured encoder across or the byte path would escape (e.g. <, >, ') where
        // the string path does not, diverging on the wire.
        using var utf8JsonWriter = new Utf8JsonWriter(writer, new JsonWriterOptions { Encoder = _jsonSerializerOptions.Encoder });
        System.Text.Json.JsonSerializer.Serialize(utf8JsonWriter, payload, type, _jsonSerializerOptions);
    }

    /// <inheritdoc />
    public virtual object? Deserialize(Type type, ReadOnlySpan<byte> payload)
    {
        var utf8JsonReader = new Utf8JsonReader(payload);
        return System.Text.Json.JsonSerializer.Deserialize(ref utf8JsonReader, type, _jsonSerializerOptions);
    }
}
