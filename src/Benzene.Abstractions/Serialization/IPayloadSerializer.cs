using System.Buffers;

namespace Benzene.Abstractions.Serialization;

/// <summary>
/// Additive byte-oriented extension of <see cref="ISerializer"/>: a serializer that can write
/// directly to a buffer and read directly from a byte span, avoiding an intermediate string
/// allocation. The string-based <see cref="ISerializer"/> members remain the universal fallback -
/// implementations backed by a binary-only wire format (e.g. Protobuf/MessagePack) may throw
/// <see cref="NotSupportedException"/> from them; that is a documented, acceptable contract for this
/// interface, not a bug.
/// </summary>
public interface IPayloadSerializer : ISerializer
{
    /// <summary>
    /// Serializes an object directly to <paramref name="writer"/> using runtime type information.
    /// </summary>
    /// <param name="type">The runtime type of the object to serialize.</param>
    /// <param name="payload">The object to serialize.</param>
    /// <param name="writer">The buffer to write the serialized bytes to.</param>
    void Serialize(Type type, object payload, IBufferWriter<byte> writer);

    /// <summary>
    /// Deserializes a byte span to an object using runtime type information.
    /// </summary>
    /// <param name="type">The runtime type to deserialize into.</param>
    /// <param name="payload">The serialized bytes.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    object? Deserialize(Type type, ReadOnlySpan<byte> payload);
}
