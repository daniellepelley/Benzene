using System.Buffers;
using System.Text;
using Benzene.Abstractions.Serialization;
using MessagePack.Resolvers;

namespace Benzene.MessagePack;

/// <summary>
/// <see cref="IPayloadSerializer"/> implementation backed by MessagePack-CSharp. MessagePack is a
/// genuinely binary format, but every Benzene transport's request/response body is a
/// <see cref="string"/> today (even <c>BenzeneMessageContext</c>'s byte-oriented path is "UTF-8
/// bytes of the string body" - not a true arbitrary-binary carrier). Rather than making the
/// string-based <see cref="ISerializer"/> members throw <see cref="NotSupportedException"/> (the
/// "binary-only formats may throw" option <see cref="IPayloadSerializer"/> documents), this
/// Base64-armors the msgpack bytes: <see cref="Serialize(Type, object)"/> produces Base64 text and
/// <see cref="Deserialize(Type, string)"/> consumes it, so MessagePack works unchanged through
/// every existing transport's string pipeline (all four HTTP transports, SQS, BenzeneMessage,
/// etc.), not just contexts with a byte-oriented body getter. The byte-oriented members delegate
/// through the same Base64 representation, so they stay consistent with the string path while
/// still genuinely exercising the byte-oriented request-mapping path wherever an
/// <c>IMessageBodyBytesGetter{TContext}</c> is registered (skipping one intermediate string alloc
/// there, nothing more - this is not a zero-copy raw-binary path).
/// </summary>
public class MessagePackSerializer : IPayloadSerializer
{
    private readonly global::MessagePack.MessagePackSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackSerializer"/> class using the
    /// contractless standard resolver, so plain POCOs (like every existing Benzene message type,
    /// none of which carry <c>[MessagePackObject]</c>/<c>[Key]</c> attributes) serialize by their
    /// public properties without needing a declared MessagePack contract - the same "just works on
    /// any POCO" expectation <c>JsonSerializer</c>/<c>XmlSerializer</c> already meet.
    /// </summary>
    public MessagePackSerializer()
        : this(global::MessagePack.MessagePackSerializerOptions.Standard
            // This serializer is a negotiable media format, so Deserialize consumes attacker-
            // controlled request bodies (content-type: application/msgpack). Standard options default
            // to MessagePackSecurity.TrustedData, under which a deeply-nested payload recurses into an
            // uncatchable StackOverflowException (process crash) and map payloads enable hash-collision
            // DoS. UntrustedData caps depth (500) and uses collision-resistant hashing - the guard
            // MessagePack-CSharp documents for any data crossing a trust boundary. Valid payloads are
            // unaffected.
            .WithSecurity(global::MessagePack.MessagePackSecurity.UntrustedData)
            .WithResolver(ContractlessStandardResolver.Instance))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackSerializer"/> class with custom
    /// options (e.g. to use attributed types with the default/standard resolver instead).
    /// </summary>
    /// <param name="options">The <see cref="global::MessagePack.MessagePackSerializerOptions"/> to use.</param>
    public MessagePackSerializer(global::MessagePack.MessagePackSerializerOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string Serialize(Type type, object payload)
    {
        return payload == null
            ? string.Empty
            : Convert.ToBase64String(global::MessagePack.MessagePackSerializer.Serialize(type, payload, _options));
    }

    /// <inheritdoc />
    public string Serialize<T>(T payload)
    {
        return payload == null ? string.Empty : Serialize(typeof(T), payload);
    }

    /// <inheritdoc />
    public object? Deserialize(Type type, string payload)
    {
        return string.IsNullOrEmpty(payload)
            ? null
            : global::MessagePack.MessagePackSerializer.Deserialize(type, (ReadOnlyMemory<byte>)Convert.FromBase64String(payload), _options);
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string payload)
    {
        var obj = Deserialize(typeof(T), payload);

        return obj == null ? default : (T)obj;
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
}
