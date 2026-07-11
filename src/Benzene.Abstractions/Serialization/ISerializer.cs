namespace Benzene.Abstractions.Serialization;

/// <summary>
/// Provides an abstraction for serializing and deserializing objects.
/// This interface enables Benzene to work with any serialization format (JSON, XML, Protobuf, etc.) in a provider-agnostic manner.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Serializes an object to a string using runtime type information.
    /// </summary>
    /// <param name="type">The runtime type of the object to serialize.</param>
    /// <param name="payload">The object to serialize.</param>
    /// <returns>The serialized string representation.</returns>
    string Serialize(Type type, object payload);

    /// <summary>
    /// Serializes a strongly-typed object to a string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="payload">The object to serialize.</param>
    /// <returns>The serialized string representation.</returns>
    string Serialize<T>(T payload);

    /// <summary>
    /// Deserializes a string to an object using runtime type information.
    /// </summary>
    /// <param name="type">The runtime type to deserialize into.</param>
    /// <param name="payload">The serialized string.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    object? Deserialize(Type type, string payload);

    /// <summary>
    /// Deserializes a string to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="payload">The serialized string.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    T? Deserialize<T>(string payload);
}
